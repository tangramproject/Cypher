// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;
using System.Security;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        TaskResult<ulong> AvailableBalance(SecureString identifier, SecureString password);
        void AddKeySet(SecureString secret, string identifier);
        KeySetDto CreateKeySet();
        CredentialsDto CreateWallet();
        SecureString NewID(int bytes = 32);
        SecureString Passphrase();
        byte[] HashPassword(SecureString passphrase);
        ulong TotalTransactionAmount(SecureString identifier, SecureString password, string stamp);
        TransactionDto LastTransaction(SecureString identifier, SecureString password, TransactionType transactionType);
        TaskResult<PurchaseDto> SortChange(Session session);
        byte[] NetworkAddress(ICoinDto coin, NetworkApiMethod networkApi = null);
        byte[] NetworkAddress(byte[] pk, NetworkApiMethod networkApi = null);
        string ProverPassword(SecureString password, int version);
        IEnumerable<string> WalletList();
        IEnumerable<BlanceSheetDto> TransactionHistory(SecureString identifier, SecureString password);
        IEnumerable<string> ListAddresses(SecureString secret, string identifier);
        IEnumerable<KeySetDto> ListKeySets(SecureString secret, string identifier);
    }
}