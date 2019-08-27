// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Dawn;
using Newtonsoft.Json;
using Sodium;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Coin;
using TangramCypher.Model;

namespace TangramCypher.Helper
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
        public static SecureString ToSecureString(this byte[] value)
        {
            var secureString = new SecureString();
            Array.ForEach(Encoding.UTF8.GetString(value).ToArray(), secureString.AppendChar);
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
        internal static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();
            if (s.Length == 0)
                return new byte[0];
            List<byte> result = new List<byte>();
            IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
            try
            {
                int i = 0;
                do
                {
                    byte b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;
                    result.Add(b);
                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
            return result.ToArray();
        }

        internal static void ZeroString(this string value)
        {
            var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
            unsafe
            {
                var pValue = (char*)handle.AddrOfPinnedObject();
                for (int index = 0; index < value.Length; index++)
                {
                    pValue[index] = char.MinValue;
                }
            }

            handle.Free();
        }

        public static ulong MulWithNaT(this ulong value) => (ulong)(value * Constant.NanoTan);

        public static double DivWithNaT(this ulong value) => Convert.ToDouble(value) / Constant.NanoTan;

        public static ulong ConvertToUInt64(this double value)
        {
            Guard.Argument(value, nameof(value)).NotZero().NotNegative();

            ulong amount;

            try
            {
                var parts = value.ToString().Split(new char[] { '.', ',' });
                var part1 = (ulong)System.Math.Truncate(value);

                if (parts.Length.Equals(1))
                    amount = part1.MulWithNaT();
                else
                {
                    var part2 = (ulong)((value - part1) * ulong.Parse("1".PadRight(parts[1].Length + 1, '0')) + 0.5);
                    amount = part1.MulWithNaT() + ulong.Parse(part2.ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return amount;
        }
    }
}
