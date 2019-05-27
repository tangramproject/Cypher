using System;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Actor
{
    public class SendPaymentDto
    {
        public CredentialsDto Credentials { get; set; }
        public double Amount { get; set; }
        public string ToAddress { get; set; }
        public bool CreateRedemptionKey { get; set; }
        public string Memo { get; set; }
    }
}
