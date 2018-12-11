using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Newtonsoft.Json;

namespace TangramCypher.Helpers
{
    public static class ExtentionMethods
    {
        public static StringContent AsJson(this object o)
          => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
        public static string ToHex(this byte[] data) => Sodium.Utilities.BinaryToHex(data);
        public static byte[] FromHex(this string hex) => Sodium.Utilities.HexToBinary(hex);
        public static byte[] ToByteArrayWithPadding(this String str)
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
    }
}
