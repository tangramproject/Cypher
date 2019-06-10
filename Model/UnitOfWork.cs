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
        private ITrackRepository trackRepository;
        private IReceiverRepository receiverRepository;
        private IPublicKeyAgreementRepository publicKeyAgreementRepository;
        private IPurchaseRepository purchaseRepository;
        private ISenderRepository senderRepository;

        public UnitOfWork(IVaultServiceClient vaultServiceClient, ILogger logger)
        {
            SetTransactionRepository(new TransactionRepository(vaultServiceClient, logger));
            SetRedemptionRepository(new RedemptionRepository(vaultServiceClient, logger));
            SetKeySetRepository(new KeySetRepository(vaultServiceClient, logger));
            SetTrackRepository(new TrackRepository(vaultServiceClient, logger));
            SetReceiverRepository(new ReceiverRepository(vaultServiceClient, logger));
            SetPublicKeyAgreementRepository(new PublicKeyAgreementRepository(vaultServiceClient, logger));
            SetPurchaseRepository(new PurchaseRepository(vaultServiceClient, logger));
            SetSenderRepository(new SenderRepository(vaultServiceClient,logger));
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

        public ITrackRepository GetTrackRepository()
        {
            return trackRepository;
        }

        private void SetTrackRepository(ITrackRepository value)
        {
            trackRepository = value;
        }

        private void SetReceiverRepository(IReceiverRepository value)
        {
            receiverRepository = value;
        }

        public IReceiverRepository GetReceiverRepository()
        {
            return receiverRepository;
        }

        public IPublicKeyAgreementRepository GetPublicKeyAgreementRepository()
        {
            return publicKeyAgreementRepository;
        }

        private void SetPublicKeyAgreementRepository(IPublicKeyAgreementRepository value)
        {
            publicKeyAgreementRepository = value;
        }

        private void SetPurchaseRepository(IPurchaseRepository value)
        {
            purchaseRepository = value;
        }
        public IPurchaseRepository GetPurchaseRepository()
        {
            return purchaseRepository;
        }

        private void SetSenderRepository(ISenderRepository value) {
            senderRepository = value;
        }

        public ISenderRepository GetSenderRepository()
        {
            return senderRepository;
        }

        public void Dispose()
        {
            // throw new NotImplementedException();
        }
    }
}