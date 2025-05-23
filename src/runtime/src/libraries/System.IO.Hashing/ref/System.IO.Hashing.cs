// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Hashing
{
    public sealed partial class Crc32 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        public Crc32() : base (default(int)) { }
        public override void Append(System.ReadOnlySpan<byte> source) { }
        public System.IO.Hashing.Crc32 Clone() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetCurrentHashAsUInt32() { throw null; }
        protected override void GetCurrentHashCore(System.Span<byte> destination) { }
        protected override void GetHashAndResetCore(System.Span<byte> destination) { }
        public static byte[] Hash(byte[] source) { throw null; }
        public static byte[] Hash(System.ReadOnlySpan<byte> source) { throw null; }
        public static int Hash(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint HashToUInt32(System.ReadOnlySpan<byte> source) { throw null; }
        public override void Reset() { }
        public static bool TryHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Crc64 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        public Crc64() : base (default(int)) { }
        public System.IO.Hashing.Crc64 Clone() { throw null; }
        public override void Append(System.ReadOnlySpan<byte> source) { }
        [System.CLSCompliantAttribute(false)]
        public ulong GetCurrentHashAsUInt64() { throw null; }
        protected override void GetCurrentHashCore(System.Span<byte> destination) { }
        protected override void GetHashAndResetCore(System.Span<byte> destination) { }
        public static byte[] Hash(byte[] source) { throw null; }
        public static byte[] Hash(System.ReadOnlySpan<byte> source) { throw null; }
        public static int Hash(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong HashToUInt64(System.ReadOnlySpan<byte> source) { throw null; }
        public override void Reset() { }
        public static bool TryHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class NonCryptographicHashAlgorithm
    {
        protected NonCryptographicHashAlgorithm(int hashLengthInBytes) { }
        public int HashLengthInBytes { get { throw null; } }
        public void Append(byte[] source) { }
        public void Append(System.IO.Stream stream) { }
        public abstract void Append(System.ReadOnlySpan<byte> source);
        public System.Threading.Tasks.Task AppendAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public byte[] GetCurrentHash() { throw null; }
        public int GetCurrentHash(System.Span<byte> destination) { throw null; }
        protected abstract void GetCurrentHashCore(System.Span<byte> destination);
        public byte[] GetHashAndReset() { throw null; }
        public int GetHashAndReset(System.Span<byte> destination) { throw null; }
        protected virtual void GetHashAndResetCore(System.Span<byte> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("Use GetCurrentHash() to retrieve the computed hash code.", true)]
        public override int GetHashCode() { throw null; }
        public abstract void Reset();
        public bool TryGetCurrentHash(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryGetHashAndReset(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class XxHash128 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        public XxHash128() : base (default(int)) { }
        public XxHash128(long seed) : base (default(int)) { }
        public override void Append(System.ReadOnlySpan<byte> source) { }
        public System.IO.Hashing.XxHash128 Clone() { throw null; }
        protected override void GetCurrentHashCore(System.Span<byte> destination) { }
        public static byte[] Hash(byte[] source) { throw null; }
        public static byte[] Hash(byte[] source, long seed) { throw null; }
        public static byte[] Hash(System.ReadOnlySpan<byte> source, long seed = (long)0) { throw null; }
        public static int Hash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, long seed = (long)0) { throw null; }
        public override void Reset() { }
        public static bool TryHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, long seed = (long)0) { throw null; }
    }
    public sealed partial class XxHash3 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        public XxHash3() : base (default(int)) { }
        public XxHash3(long seed) : base (default(int)) { }
        public override void Append(System.ReadOnlySpan<byte> source) { }
        public System.IO.Hashing.XxHash3 Clone() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetCurrentHashAsUInt64() { throw null; }
        protected override void GetCurrentHashCore(System.Span<byte> destination) { }
        public static byte[] Hash(byte[] source) { throw null; }
        public static byte[] Hash(byte[] source, long seed) { throw null; }
        public static byte[] Hash(System.ReadOnlySpan<byte> source, long seed = (long)0) { throw null; }
        public static int Hash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, long seed = (long)0) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong HashToUInt64(System.ReadOnlySpan<byte> source, long seed = (long)0) { throw null; }
        public override void Reset() { }
        public static bool TryHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, long seed = (long)0) { throw null; }
    }
    public sealed partial class XxHash32 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        public XxHash32() : base (default(int)) { }
        public XxHash32(int seed) : base (default(int)) { }
        public override void Append(System.ReadOnlySpan<byte> source) { }
        public System.IO.Hashing.XxHash32 Clone() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetCurrentHashAsUInt32() { throw null; }
        protected override void GetCurrentHashCore(System.Span<byte> destination) { }
        public static byte[] Hash(byte[] source) { throw null; }
        public static byte[] Hash(byte[] source, int seed) { throw null; }
        public static byte[] Hash(System.ReadOnlySpan<byte> source, int seed = 0) { throw null; }
        public static int Hash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, int seed = 0) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint HashToUInt32(System.ReadOnlySpan<byte> source, int seed = 0) { throw null; }
        public override void Reset() { }
        public static bool TryHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, int seed = 0) { throw null; }
    }
    public sealed partial class XxHash64 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        public XxHash64() : base (default(int)) { }
        public XxHash64(long seed) : base (default(int)) { }
        public override void Append(System.ReadOnlySpan<byte> source) { }
        public System.IO.Hashing.XxHash64 Clone() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetCurrentHashAsUInt64() { throw null; }
        protected override void GetCurrentHashCore(System.Span<byte> destination) { }
        public static byte[] Hash(byte[] source) { throw null; }
        public static byte[] Hash(byte[] source, long seed) { throw null; }
        public static byte[] Hash(System.ReadOnlySpan<byte> source, long seed = (long)0) { throw null; }
        public static int Hash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, long seed = (long)0) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong HashToUInt64(System.ReadOnlySpan<byte> source, long seed = (long)0) { throw null; }
        public override void Reset() { }
        public static bool TryHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, long seed = (long)0) { throw null; }
    }
}
