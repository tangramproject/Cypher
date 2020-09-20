// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using NBitcoin;
using TGMWalletCore.Actor;
using TGMWalletCore.Helper;
using TGMWalletCore.Model;
using Transaction = TGMWalletCore.Model.Transaction;

namespace TGMWalletCore.Wallet
{
    public interface IWalletService
    {
        TaskResult<ulong> AvailableBalance(SecureString identifier, SecureString passphrase);
        void AddKeySet(SecureString identifier, SecureString passphrase);
        KeySet  CreateKeySet(string path, byte[] secKey, byte[] chainCode);
        string CreateWallet(SecureString mnemonic, SecureString passphrase);
        SecureString NewID(int bytes = 32);
        Task<string[]> CreateMnemonic(Language language, WordCount wordCount);
        byte[] HashPassphrase(SecureString passphrase);
        ulong TotalTransactionAmount(SecureString identifier, SecureString passphrase, string address);
        Transaction  LastTransaction(SecureString identifier, SecureString passphrase, TransactionType transactionType);
        TaskResult<Transaction> SortChange(Session session);
        IEnumerable<string> WalletList();
        IEnumerable<BlanceSheet> TransactionHistory(SecureString identifier, SecureString passphrase);
        IEnumerable<string> ListAddresses(SecureString identifier, SecureString passphrase);
        IEnumerable<KeySet> ListKeySets(SecureString identifier, SecureString passphrase);
        KeySet NextKeySet(SecureString identifier, SecureString passphrase);
    }
}