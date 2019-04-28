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
        Task<double> AvailableBalanceFromStamp(SecureString identifier, SecureString password, string stamp);
        Task<double> AvailableBalanceGeneric(SecureString identifier, SecureString password);
        Task<bool> AddKey(SecureString identifier, SecureString password, PkSkDto pkSk);
        PkSkDto CreatePkSk();
        SecureString NewID(int bytes = 32);
        SecureString Passphrase();
        byte[] HashPassword(SecureString passphrase);
        Task<bool> AddTransaction(SecureString identifier, SecureString password, TransactionDto transaction);
        Task<TransactionDto> Transaction(SecureString identifier, SecureString password, string hash);
        Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password);
        Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password, string stamp);
        Task<double> TransactionAmount(SecureString identifier, SecureString password, string stamp);
        Task<SecureString> StoreKey(SecureString identifier, SecureString password, string storeKey);
        Task<SecureString> StoreKey(SecureString identifier, SecureString password, StoreKeyApiMethod storeKeyApi, string address);
        Task<TransactionIndicator> SortChange(SecureString identifier, SecureString password, double amount);
        Task<bool> AddMessageTracking(SecureString identifier, SecureString password, MessageTrackDto messageTrack);
        Task<MessageTrackDto> MessageTrack(SecureString identifier, SecureString password, string pk);
        byte[] NetworkAddress(CoinDto coin, NetworkApiMethod networkApi = null);
        byte[] NetworkAddress(byte[] pk, NetworkApiMethod networkApi = null);
        string ProverPassword(SecureString password, int version);
        Task<bool> ClearTransactions(SecureString identifier, SecureString password);
        Task<string> RandomAddress(SecureString identifier, SecureString password);
    }
}