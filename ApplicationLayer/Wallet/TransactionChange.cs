using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class TransactionChange
    {
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
        public TransactionDto Transaction { get; set; }
        public double AmountFor { get; set; }
    }
}
