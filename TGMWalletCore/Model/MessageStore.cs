// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;

namespace TGMWalletCore.Model
{
    public class MessageStore
    {
        public DateTime DateTime { get; set; }
        public string Hash { get; set; }
        public string Memo { get; set; }
        public Message  Message { get; set; }
        public string PublicKey { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}
