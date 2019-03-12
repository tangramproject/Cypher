using System;
using System.Runtime.InteropServices;
using System.Security;

namespace TangramCypher.Helper.LibSodium
{
    internal static class SodiumPadding
    {
#if __IOS__ || (UNITY_IOS && !UNITY_EDITOR)
            private const string nativeLibrary = "__Internal";
#else
        private const string nativeLibrary = "libsodium";
#endif

        [SuppressUnmanagedCodeSecurity]
        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sodium_pad")]
        internal static extern int Pad(ulong padded_buflen_p, byte buf, ulong unpadded_buflen, ulong blocksize, ulong max_buflen);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sodium_unpad")]
        internal static extern int Unpad(ulong unpadded_buflen_p, byte buf, ulong padded_buflen, ulong blocksize);
    }
}
