// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal
{
    public static partial class Console
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static unsafe void Write(string s)
        {
            Interop.Logcat.AndroidLogPrint(Interop.Logcat.LogLevel.Debug, "DOTNET", s ?? string.Empty);
        }

        public static partial class Error
        {
            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static unsafe void Write(string s)
            {
                Interop.Logcat.AndroidLogPrint(Interop.Logcat.LogLevel.Error, "DOTNET", s ?? string.Empty);
            }
        }
    }
}
