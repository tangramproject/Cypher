using System;
using SimpleBase;
using System.Collections.Generic;
using TangramCypher.ApplicationLayer.Actor;
using System.Text;
using System.Linq;

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
    }
}
