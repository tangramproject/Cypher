using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        Task<double> GetBalance(SecureString identifier, SecureString password);
        PkSkDto CreatePkSk();
        SecureString NewID(int bytes = 32);
        SecureString Passphrase();
        byte[] HashPassword(SecureString passphrase);
        Task AddTransaction(SecureString identifier, SecureString password, TransactionDto transaction);
        Task<TransactionDto> GetTransaction(SecureString identifier, SecureString password, string hash);
        Task<List<TransactionDto>> GetTransactions(SecureString identifier, SecureString password);
        Task<double> GetTransactionAmount(SecureString identifier, SecureString password, string stamp);
        Task<SecureString> GetStoreKey(SecureString identifier, SecureString password, string storeKey);
        Task<TransactionChange> MakeChange(SecureString identifier, SecureString password, double amount);
    }
}