using System;
using SimpleBase;
using System.Collections.Generic;
using TangramCypher.ApplicationLayer.Actor;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Net;
using System.Security;

namespace TangramCypher.Helpers
{
    public static class Util
    {
        internal static Random _Random = new Random();

        public static string ToHex(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        public static string Pop(string value, string delimiter)
        {
            var stack = new Stack<string>(value.Split(new string[] { delimiter }, StringSplitOptions.None));
            return stack.Pop();
        }

        public static CommitmentKeyDto FreeCommitmentKey(string base58Key)
        {
            var base58 = Base58.Bitcoin.Decode(base58Key);
            var proof = Encoding.UTF8.GetString(base58).Substring(0, 64);
            var key1 = Encoding.UTF8.GetString(base58).Substring(64, 128);
            var key2 = Encoding.UTF8.GetString(base58).Substring(128, 192);

            return new CommitmentKeyDto() { Key1 = key1, Key2 = key1, Proof = proof }; ;
        }

        public static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public static void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                int r = i + _Random.Next(n - i);
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
        }

        public static OSPlatform GetOSPlatform()
        {
            OSPlatform osPlatform = OSPlatform.Create("Other Platform");
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            osPlatform = isWindows ? OSPlatform.Windows : osPlatform;

            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            osPlatform = isOSX ? OSPlatform.OSX : osPlatform;

            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            osPlatform = isLinux ? OSPlatform.Linux : osPlatform;

            return osPlatform;
        }

        public static string EntryAssemblyPath() {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static string ToPlainString(SecureString secure)
        {
            return new NetworkCredential(string.Empty, secure).Password;
        }
    }
}
