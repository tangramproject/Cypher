// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Dawn;
using TGMWalletCore.Helper;

namespace TGMWalletCore.LibSodium
{
    public static class Crypto
    {
        public static byte[] RandomBytes(int bytes = 32)
        {
            Guard.Argument(bytes, nameof(bytes)).NotNegative();

            using(var rng = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[bytes];
                rng.GetBytes(randomBytes);
                return randomBytes;
            }
        }
    }
}