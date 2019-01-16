using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using Sodium;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.Helpers
{
    public static class ExtentionMethods
    {
        public static StringContent AsJson(this object o)
          => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
        public static string ToHex(this byte[] data) => Utilities.BinaryToHex(data);
        public static byte[] FromHex(this string hex) => Utilities.HexToBinary(hex);
        public static string ToBase64(this byte[] data) => Convert.ToBase64String(Encoding.UTF8.GetBytes(Utilities.BinaryToHex(data)));
        public static byte[] ToByteArrayWithPadding(this string str)
        {
            const int BlockingSize = 16;
            int byteLength = ((str.Length / BlockingSize) + 1) * BlockingSize;
            byte[] toEncrypt = new byte[byteLength];
            Encoding.ASCII.GetBytes(str).CopyTo(toEncrypt, 0);
            return toEncrypt;
        }
        public static string RemovePadding(this String str)
        {
            char paddingChar = '\0';
            int indexOfFirstPadding = str.IndexOf(paddingChar);
            string cleanString = str.Remove(indexOfFirstPadding);
            return cleanString;
        }
        public static void ExecuteInConstrainedRegion(this Action action)
        {
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                action();
            }
        }
        public static SecureString ToSecureString(this string value)
        {
            var secureString = new SecureString();
            Array.ForEach(value.ToArray(), secureString.AppendChar);
            secureString.MakeReadOnly();
            return secureString;
        }
        public static string ToUnSecureString(this SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        public static T ToArray<T>(this SecureString src, Func<byte[], T> func)
        {
            IntPtr bstr = IntPtr.Zero;
            byte[] workArray = null;
            GCHandle handle = GCHandle.Alloc(workArray, GCHandleType.Pinned);
            try
            {
                bstr = Marshal.SecureStringToBSTR(src);
                unsafe
                {
                    byte* bstrBytes = (byte*)bstr;
                    workArray = new byte[src.Length * 2];

                    for (int i = 0; i < workArray.Length; i++)
                        workArray[i] = *bstrBytes++;
                }

                return func(workArray);
            }
            finally
            {
                if (workArray != null)
                    for (int i = 0; i < workArray.Length; i++)
                        workArray[i] = 0;
                handle.Free();
                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);
            }
        }
        public static CoinDto FormatCoinToBase64(this CoinDto coin)
        {
            var formattedCoin = new CoinDto
            {
                Envelope = new EnvelopeDto()
                {
                    Amount = coin.Envelope.Amount,
                    Serial = Convert.ToBase64String(Encoding.UTF8.GetBytes(coin.Envelope.Serial))
                }
            };
            formattedCoin.Hint = Convert.ToBase64String(Encoding.UTF8.GetBytes(coin.Hint));
            formattedCoin.Keeper = Convert.ToBase64String(Encoding.UTF8.GetBytes(coin.Keeper));
            formattedCoin.Principle = Convert.ToBase64String(Encoding.UTF8.GetBytes(coin.Principle));
            formattedCoin.Stamp = Convert.ToBase64String(Encoding.UTF8.GetBytes(coin.Stamp));
            formattedCoin.Version = coin.Version;

            return formattedCoin;
        }
        public static CoinDto FormatCoinFromBase64(this CoinDto coin)
        {
            var formattedCoin = new CoinDto
            {
                Envelope = new EnvelopeDto()
                {
                    Amount = coin.Envelope.Amount,
                    Serial = Encoding.UTF8.GetString(Convert.FromBase64String(coin.Envelope.Serial))
                }
            };
            formattedCoin.Hint = Encoding.UTF8.GetString(Convert.FromBase64String(coin.Hint));
            formattedCoin.Keeper = Encoding.UTF8.GetString(Convert.FromBase64String(coin.Keeper));
            formattedCoin.Principle = Encoding.UTF8.GetString(Convert.FromBase64String(coin.Principle));
            formattedCoin.Stamp = Encoding.UTF8.GetString(Convert.FromBase64String(coin.Stamp));
            formattedCoin.Version = coin.Version;

            return formattedCoin;
        }
    }
}
