using System.Collections.Generic;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class TransactionChange
    {
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
        public TransactionDto Transaction { get; set; }
        public double AmountFor { get; set; }
    }
}
