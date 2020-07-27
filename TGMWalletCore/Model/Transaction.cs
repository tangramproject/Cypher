// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;

namespace TGMWalletCore.Model
{
    public class Transaction
    {
        public string Address { get; set; }
        public ulong Balance { get; set; }
        public DateTime DateTime { get; set; }
        public string EphemKey { get; set; }
        public ulong Input { get; set; }
        public string Memo { get; set; }
        public ulong Output { get; set; }
        public bool Spent { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
        public TransactionType TransactionType { get; set; }
    }
}