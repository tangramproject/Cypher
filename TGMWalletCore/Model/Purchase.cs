// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;

namespace TGMWalletCore.Model
{
    public class Purchase
    {
        public ulong Balance { get; set; }
        public DateTime DateTime { get; set; }
        public string EphemKey { get; set; }
        public ulong Input { get; set; }
        public ulong Output { get; set; }
        public bool Spent { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}
