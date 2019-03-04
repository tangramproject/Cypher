using System;
namespace TangramCypher.ApplicationLayer.Wallet
{
    public enum TransactionType
    {
        Send,
        Receive
    }

    public class TransactionDto
    {
        public double Amount { get; set; }
        public string Commitment { get; set; }
        public string Hash { get; set; }
        public string Stamp { get; set; }
        public int Version { get; set; }
        public TransactionType TransactionType { get; set; }
    }
}
