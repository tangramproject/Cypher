// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;

namespace TGMWalletCore.Model
{

    public class Queue
    {
        public DateTime DateTime { get; set; }
        public bool PaymentFailed { get; set; }
        public bool PublicAgreementFailed { get; set; }
        public bool ReceiverFailed { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}