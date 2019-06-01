// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using Microsoft.Extensions.Logging;
using TangramCypher.ApplicationLayer.Vault;

namespace TangramCypher.Model
{
    public class TransactionRepository : Repository<TransactionDto>, ITransactionRepository
    {
        public TransactionRepository(IVaultServiceClient vaultServiceClient, ILogger logger)
            : base(StoreName.Transactions, vaultServiceClient, logger)
        {
        }
    }
}