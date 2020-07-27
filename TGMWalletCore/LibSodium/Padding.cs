// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Runtime.InteropServices;
using System.Security;

namespace TGMWalletCore.LibSodium
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
        internal static extern int Pad(ref ulong padded_buflen_p, byte[] buf, ulong unpadded_buflen, ulong blocksize, ulong max_buflen);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sodium_unpad")]
        internal static extern int Unpad(ref ulong unpadded_buflen_p, byte[] buf, ulong padded_buflen, ulong blocksize);
    }
}
