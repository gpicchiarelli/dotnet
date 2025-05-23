// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DllImportCallback.cpp
//

//


#include "common.h"

#include "threads.h"
#include "excep.h"
#include "object.h"
#include "dllimportcallback.h"
#include "mlinfo.h"
#include "ceeload.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "appdomain.inl"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

struct UM2MThunk_Args
{
    UMEntryThunk *pEntryThunk;
    void *pAddr;
    void *pThunkArgs;
    int argLen;
};

class UMEntryThunkFreeList
{
public:
    UMEntryThunkFreeList(size_t threshold) :
        m_threshold(threshold),
        m_count(0),
        m_pHead(NULL),
        m_pTail(NULL)
    {
        WRAPPER_NO_CONTRACT;

        m_crst.Init(CrstUMEntryThunkFreeListLock, CRST_UNSAFE_ANYMODE);
    }

    UMEntryThunk *GetUMEntryThunk()
    {
        WRAPPER_NO_CONTRACT;

        if (m_count < m_threshold)
            return NULL;

        CrstHolder ch(&m_crst);

        UMEntryThunk *pThunk = m_pHead;

        if (pThunk == NULL)
            return NULL;

        m_pHead = m_pHead->GetData()->m_pNextFreeThunk;
        --m_count;

        return pThunk;
    }

    void AddToList(UMEntryThunk *pThunk)
    {
        CONTRACTL
        {
            NOTHROW;
        }
        CONTRACTL_END;

        CrstHolder ch(&m_crst);

        if (m_pHead == NULL)
        {
            m_pHead = pThunk;
            m_pTail = pThunk;
        }
        else
        {
            m_pTail->GetData()->m_pNextFreeThunk = pThunk;
            m_pTail = pThunk;
        }

        pThunk->GetData()->m_pNextFreeThunk = NULL;

        ++m_count;
    }

private:
    // Used to delay reusing freed thunks
    size_t m_threshold;
    size_t m_count;
    UMEntryThunk *m_pHead;
    UMEntryThunk *m_pTail;
    CrstStatic m_crst;
};

#define DEFAULT_THUNK_FREE_LIST_THRESHOLD 64

static UMEntryThunkFreeList s_thunkFreeList(DEFAULT_THUNK_FREE_LIST_THRESHOLD);

PCODE UMThunkMarshInfo::GetExecStubEntryPoint()
{
    LIMITED_METHOD_CONTRACT;

    return m_pILStub;
}

UMEntryThunkCache::UMEntryThunkCache(AppDomain *pDomain) :
    m_crst(CrstUMEntryThunkCache),
    m_pDomain(pDomain)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pDomain != NULL);
}

UMEntryThunkCache::~UMEntryThunkCache()
{
    WRAPPER_NO_CONTRACT;

    for (SHash<ThunkSHashTraits>::Iterator i = m_hash.Begin(); i != m_hash.End(); i++)
    {
        // UMEntryThunks in this cache own UMThunkMarshInfo in 1-1 fashion
        DestroyMarshInfo(i->m_pThunk->GetUMThunkMarshInfo());
        UMEntryThunk::FreeUMEntryThunk(i->m_pThunk);
    }
}

UMEntryThunk *UMEntryThunkCache::GetUMEntryThunk(MethodDesc *pMD)
{
    CONTRACT (UMEntryThunk *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunk *pThunk;

    CrstHolder ch(&m_crst);

    const CacheElement *pElement = m_hash.LookupPtr(pMD);
    if (pElement != NULL)
    {
        pThunk = pElement->m_pThunk;
    }
    else
    {
        // cache miss -> create a new thunk
        pThunk = UMEntryThunk::CreateUMEntryThunk();
        Holder<UMEntryThunk *, DoNothing, UMEntryThunk::FreeUMEntryThunk> umHolder;
        umHolder.Assign(pThunk);

        UMThunkMarshInfo *pMarshInfo = (UMThunkMarshInfo *)(void *)(m_pDomain->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(UMThunkMarshInfo))));
        Holder<UMThunkMarshInfo *, DoNothing, UMEntryThunkCache::DestroyMarshInfo> miHolder;
        miHolder.Assign(pMarshInfo);

        pMarshInfo->LoadTimeInit(pMD);

        pThunk->LoadTimeInit((PCODE)NULL, NULL, pMarshInfo, pMD);

        // add it to the cache
        CacheElement element;
        element.m_pMD = pMD;
        element.m_pThunk = pThunk;
        m_hash.Add(element);

        miHolder.SuppressRelease();
        umHolder.SuppressRelease();
    }

    RETURN pThunk;
}

// FailFast if a method marked UnmanagedCallersOnlyAttribute is
// invoked directly from managed code. UMThunkStub.asm check the
// mode and call this function to failfast.
extern "C" VOID STDCALL ReversePInvokeBadTransition()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    // Fail
    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(
                                             COR_E_EXECUTIONENGINE,
                                             W("Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code.")
                                            );
}

PCODE TheUMEntryPrestubWorker(UMEntryThunkData * pUMEntryThunkData)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    Thread * pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        CREATETHREAD_IF_NULL_FAILFAST(pThread, W("Failed to setup new thread during reverse P/Invoke"));
    }

    UMEntryThunk* pUMEntryThunk = pUMEntryThunkData->m_pUMEntryThunk;

    // Verify the current thread isn't in COOP mode.
    if (pThread->PreemptiveGCDisabled())
        ReversePInvokeBadTransition();

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this method is called by stubs which are called by managed code,
    // so we need an unwind and continue handler so that our internal
    // exceptions don't leak out into managed code.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    pUMEntryThunk->RunTimeInit();

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    return (PCODE)pUMEntryThunk->GetCode();
}

UMEntryThunk* UMEntryThunk::CreateUMEntryThunk()
{
    CONTRACT (UMEntryThunk*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunk * p;

    p = s_thunkFreeList.GetUMEntryThunk();

    if (p == NULL)
    {
        static_assert_no_msg(sizeof(UMEntryThunk) == sizeof(StubPrecode));
        LoaderAllocator *pLoaderAllocator = SystemDomain::GetGlobalLoaderAllocator();
        AllocMemTracker amTracker;
        AllocMemTracker *pamTracker = &amTracker;
       
        UMEntryThunkData *pData = (UMEntryThunkData *)pamTracker->Track(pLoaderAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(UMEntryThunkData))));
        p = (UMEntryThunk*)pamTracker->Track(pLoaderAllocator->GetNewStubPrecodeHeap()->AllocStub());
#ifdef FEATURE_PERFMAP
        PerfMap::LogStubs(__FUNCTION__, "UMEntryThunk", (PCODE)p, sizeof(UMEntryThunk), PerfMapStubType::IndividualWithinBlock);
#endif
        pData->m_pUMEntryThunk = p;
        p->Init(p, dac_cast<TADDR>(pData), NULL, dac_cast<TADDR>(PRECODE_UMENTRY_THUNK));
        pamTracker->SuppressRelease();
    }

    RETURN p;
}

void UMEntryThunk::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    SetTargetUnconditional((TADDR)UMEntryThunk::ReportViolation);

    if (GetObjectHandle())
    {
        DestroyLongWeakHandle(GetObjectHandle());
        GetData()->m_pObjectHandle = 0;
    }

    s_thunkFreeList.AddToList(this);
}

VOID UMEntryThunk::FreeUMEntryThunk(UMEntryThunk* p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(p));
    }
    CONTRACTL_END;

    p->Terminate();
}


//-------------------------------------------------------------------------
// This function is used to report error when we call collected delegate.
// But memory that was allocated for thunk can be reused, due to it this
// function will not be called in all cases of the collected delegate call,
// also it may crash while trying to report the problem.
//-------------------------------------------------------------------------
VOID __fastcall UMEntryThunk::ReportViolation(UMEntryThunkData* pEntryThunkData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pEntryThunkData));
    }
    CONTRACTL_END;

    UMEntryThunk* pEntryThunk = pEntryThunkData->m_pUMEntryThunk;

    MethodDesc* pMethodDesc = pEntryThunk->GetMethod();

    SString namespaceOrClassName;
    SString methodName;
    pMethodDesc->GetMethodInfoNoSig(namespaceOrClassName, methodName);

    SString message;
    message.Printf("A callback was made on a garbage collected delegate of type '%s!%s::%s'.",
        pMethodDesc->GetModule()->GetSimpleName(),
        namespaceOrClassName.GetUTF8(),
        methodName.GetUTF8());

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, message.GetUnicode());
}

UMThunkMarshInfo::~UMThunkMarshInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    FillMemory(this, sizeof(*this), 0xcc);
#endif
}

MethodDesc* UMThunkMarshInfo::GetILStubMethodDesc(MethodDesc* pInvokeMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStubMD = NULL;
    dwStubFlags |= NDIRECTSTUB_FL_REVERSE_INTEROP;  // could be either delegate interop or not--that info is passed in from the caller

#if defined(DEBUGGING_SUPPORTED)
    // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
    CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(pSigInfo->GetModule(), CORJIT_FLAGS());
    if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
    {
        dwStubFlags |= NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL;
    }
#endif // DEBUGGING_SUPPORTED

    pStubMD = NDirect::CreateCLRToNativeILStub(
        pSigInfo,
        dwStubFlags,
        pInvokeMD // may be NULL
        );

    return pStubMD;
}

//----------------------------------------------------------
// This initializer is called during load time.
// It does not do any stub initialization or sigparsing.
// The RunTimeInit() must be called subsequently to fully
// UMThunkMarshInfo.
//----------------------------------------------------------
VOID UMThunkMarshInfo::LoadTimeInit(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(pMD != NULL);

    LoadTimeInit(pMD->GetSignature(), pMD->GetModule(), pMD);
}

VOID UMThunkMarshInfo::LoadTimeInit(Signature sig, Module * pModule, MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;

    FillMemory(this, sizeof(UMThunkMarshInfo), 0); // Prevent problems with partial deletes

    // This will be overwritten by the actual code pointer (or NULL) at the end of UMThunkMarshInfo::RunTimeInit()
    m_pILStub = (PCODE)1;

    m_pMD = pMD;
    m_pModule = pModule;
    m_sig = sig;
}

//----------------------------------------------------------
// This initializer finishes the init started by LoadTimeInit.
// It does stub creation and can throw an exception.
//
// It can safely be called multiple times and by concurrent
// threads.
//----------------------------------------------------------
VOID UMThunkMarshInfo::RunTimeInit()
{
    STANDARD_VM_CONTRACT;

    // Nothing to do if already inited
    if (IsCompletelyInited())
        return;

    MethodDesc * pMD = GetMethod();

    PInvokeStaticSigInfo sigInfo;

    if (pMD != NULL)
        new (&sigInfo) PInvokeStaticSigInfo(pMD);
    else
        new (&sigInfo) PInvokeStaticSigInfo(GetSignature(), GetModule());

    DWORD dwStubFlags = 0;

    if (sigInfo.IsDelegateInterop())
        dwStubFlags |= NDIRECTSTUB_FL_DELEGATE;

    MethodDesc* pStubMD = GetILStubMethodDesc(pMD, &sigInfo, dwStubFlags);
    PCODE pFinalILStub = JitILStub(pStubMD);

    // Must be the last thing we set!
    InterlockedCompareExchangeT<PCODE>(&m_pILStub, pFinalILStub, (PCODE)1);
}

#ifdef _DEBUG
void STDCALL LogUMTransition(UMEntryThunk* thunk)
{
    CONTRACTL
    {
        NOTHROW;
        DEBUG_ONLY;
        GC_NOTRIGGER;
        ENTRY_POINT;
        if (GetThreadNULLOk()) MODE_PREEMPTIVE; else MODE_ANY;
        DEBUG_ONLY;
        PRECONDITION(CheckPointer(thunk));
        PRECONDITION((GetThreadNULLOk() != NULL) ? (!GetThread()->PreemptiveGCDisabled()) : TRUE);
    }
    CONTRACTL_END;

    void** retESP = ((void**) &thunk) + 4;

    MethodDesc* method = thunk->GetMethod();
    if (method)
    {
        LOG((LF_STUBS, LL_INFO1000000, "UNMANAGED -> MANAGED Stub To Method = %s::%s SIG %s Ret Address ESP = 0x%x ret = 0x%x\n",
            method->m_pszDebugClassName,
            method->m_pszDebugMethodName,
            method->m_pszDebugMethodSignature, retESP, *retESP));
    }
}
#endif // _DEBUG

