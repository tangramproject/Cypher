// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;

namespace TangramCypher.Model
{
    public class TransactionRepository : Repository<TransactionDto>, ITransactionRepository
    {
        public TransactionRepository(IVaultServiceClient vaultServiceClient, ILogger logger)
            : base(StoreName.Transactions, vaultServiceClient, logger)
        {
        }

        public ulong Sum(IEnumerable<ulong> source)
        {
            var sum = 0UL;
            foreach (var number in source)
            {
                sum += number;
            }
            return sum;
        }

        public ulong Sum(IEnumerable<TransactionDto> source, TransactionType transactionType)
        {
            var amounts = source.Where(tx => tx.TransactionType == transactionType).Select(p => p.Amount);
            var sum = 0UL;

            foreach (var amount in amounts)
            {
                sum += amount;
            }
            return sum;
        }
    }
}