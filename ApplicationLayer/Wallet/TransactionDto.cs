using System;
namespace TangramCypher.ApplicationLayer.Wallet
{
    public class TransactionDto
    {
        public double Amount { get; set; }
        public string Commitment { get; set; }
        public string Hash { get; set; }
        public string Stamp { get; set; }
        public int Version { get; set; }
    }

}
