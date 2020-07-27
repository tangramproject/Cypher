// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;
using ProtoBuf;

namespace TGMWalletCore.Model
{
    [ProtoContract]
    public class Coin : ICoin
    {
        [BsonId]
        public Guid TransactionId { get; set; }

        [ProtoMember(1)]
        public int Ver { get; set; }
        [ProtoMember(2)]
        public string PreImage { get; set; }
        [ProtoMember(3)]
        public int Mix { get; set; }
        [ProtoMember(4)]
        public Vin Vin { get; set; }
        [ProtoMember(5)]
        public Vout Vout { get; set; }
    }
}
