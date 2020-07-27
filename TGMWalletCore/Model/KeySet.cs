// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using LiteDB;

namespace TGMWalletCore.Model
{
    public class KeySet
    {
        public string ChainCode { get; set; }
        public string[] Paths { get; set; }
        public string RootKey { get; set; }
        [BsonId]
        public string StealthAddress { get; set; }
    }
}