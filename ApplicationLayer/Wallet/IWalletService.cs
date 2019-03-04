using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        Task<double> AvailableBalance(SecureString identifier, SecureString password);
        PkSkDto CreatePkSk();
        SecureString NewID(int bytes = 32);
        SecureString Passphrase();
        byte[] HashPassword(SecureString passphrase);
        Task AddTransaction(SecureString identifier, SecureString password, TransactionDto transaction);
        Task<TransactionDto> Transaction(SecureString identifier, SecureString password, string hash);
        Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password);
        Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password, string stamp);
        Task<double> TransactionAmount(SecureString identifier, SecureString password, string stamp);
        Task<SecureString> StoreKey(SecureString identifier, SecureString password, string storeKey);
        Task<TransactionChange> MakeChange(SecureString identifier, SecureString password, double amount);
        Task<TransactionChange> MakeChange(SecureString identifier, SecureString password, double amount, string stamp);
        Task AddMessageTracking(SecureString identifier, SecureString password, MessageTrackDto messageTrack);
        Task<MessageTrackDto> MessageTrack(SecureString identifier, SecureString password, string pk);
    }
}