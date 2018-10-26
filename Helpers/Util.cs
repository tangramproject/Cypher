using System;
using SimpleBase;
using System.Collections.Generic;
using TangramCypher.ApplicationLayer.Actor;
using System.Text;

namespace TangramCypher.Helpers
{
    public static class Util
    {
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
    }
}
