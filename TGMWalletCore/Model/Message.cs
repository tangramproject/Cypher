// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;
using ProtoBuf;

namespace TGMWalletCore.Model
{
    [ProtoContract]
    public class Message
    {
        [ProtoMember(1)]
        public string Address { get; set; }
        [ProtoMember(2)]
        public string Body { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}