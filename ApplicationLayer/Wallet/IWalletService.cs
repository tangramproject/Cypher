// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Coin;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        Task<ulong> AvailableBalance(SecureString identifier, SecureString password);
        PkSkDto CreatePkSk();
        Task<CredentialsDto> CreateWallet();
        SecureString NewID(int bytes = 32);
        SecureString Passphrase();
        byte[] HashPassword(SecureString passphrase);
        Task<TransactionDto> Transaction(SecureString identifier, SecureString password, string hash);
        Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password);
        Task<ulong> TotalTransactionAmount(SecureString identifier, SecureString password, string stamp);
        Task<TransactionDto> LastTransaction(SecureString identifier, SecureString password, TransactionType transactionType);
        Task<SecureString> StoreKey(SecureString identifier, SecureString password, string storeKey);
        Task<SecureString> StoreKey(SecureString identifier, SecureString password, StoreKeyApiMethod storeKeyApi, string address);
        Task<TransactionCoin> SortChange(SecureString identifier, SecureString password, ulong amount);
        Task<bool> AddMessageTracking(SecureString identifier, SecureString password, MessageTrackDto messageTrack);
        Task<MessageTrackDto> MessageTrack(SecureString identifier, SecureString password, string pk);
        byte[] NetworkAddress(CoinDto coin, NetworkApiMethod networkApi = null);
        byte[] NetworkAddress(byte[] pk, NetworkApiMethod networkApi = null);
        string ProverPassword(SecureString password, int version);
        Task<bool> ClearTransactions(SecureString identifier, SecureString password);
        Task<string> RandomAddress(SecureString identifier, SecureString password);
        Task<string> Profile(SecureString identifier, SecureString password);
        Task<IEnumerable<string>> WalletList();
        ulong MulWithNaT(ulong value);
        ulong DivWithNaT(ulong value);
        Task<bool> Put<T>(SecureString identifier, SecureString password, string key, T value, string storeName, string keyName);
    }
}