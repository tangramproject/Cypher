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
    public class UnitOfWork : IUnitOfWork
    {
        private ITransactionRepository transactionRepository;
        private IRedemptionRepository redemptionRepository;
        private IKeySetRepository keySetRepository;

        public UnitOfWork(IVaultServiceClient vaultServiceClient, ILogger logger)
        {
            SetTransactionRepository(new TransactionRepository(vaultServiceClient, logger));
            SetRedemptionRepository(new RedemptionRepository(vaultServiceClient, logger));
            SetKeySetRepository(new KeySetRepository(vaultServiceClient, logger));
        }

        public ITransactionRepository GetTransactionRepository()
        {
            return transactionRepository;
        }

        private void SetTransactionRepository(ITransactionRepository value)
        {
            transactionRepository = value;
        }

        public IRedemptionRepository GetRedemptionRepository()
        {
            return redemptionRepository;
        }

        private void SetRedemptionRepository(IRedemptionRepository value)
        {
            redemptionRepository = value;
        }

        public IKeySetRepository GetKeySetRepository()
        {
            return keySetRepository;
        }

        private void SetKeySetRepository(IKeySetRepository value)
        {
            keySetRepository = value;
        }
        
        public void Dispose()
        {
            // throw new NotImplementedException();
        }
    }
}