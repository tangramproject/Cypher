// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;
using System.Security;
using Secp256k1_ZKP.Net;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Coin
{
    public interface ICoinService
    {
        CoinService BuildReceiver();
        CoinService BuildSender();
        void ClearCache();
        (CoinDto, CoinDto) CoinSwap(SecureString password, CoinDto coin, RedemptionKeyDto redemptionKey);
        byte[] Commit(ulong amount, int version, string stamp, SecureString password);
        byte[] Commit(ulong amount);
        byte[] Commit(ulong amount, byte[] blind);
        CoinDto Coin();
        CoinDto DeriveCoin(SecureString password, CoinDto coin);
        CoinDto DeriveCoin(CoinDto coin);
        byte[] DeriveKey(double amount, string stamp, int version);
        string DeriveKey(int version, string stamp, SecureString password, int bytes = 32);
        string NewStamp();
        byte[] Hash(CoinDto coin);
        (string, string) HotRelease(int version, string stamp, SecureString password);
        IEnumerable<CoinDto> MakeMultipleCoins(IEnumerable<TransactionDto> transactions, SecureString password);
        CoinDto MakeSingleCoin(TransactionDto transaction, SecureString password);
        void MakeSingleCoin();
        ProofStruct ProofStruct();
        string PartialRelease(int version, string stamp, string memo, SecureString password);
        SecureString Password();
        CoinService Password(SecureString password);
        byte[] Sign(ulong amount, int version, string stamp, SecureString password, byte[] msg);
        byte[] Sign(ulong amount, byte[] msg);
        byte[] SignWithBlinding(byte[] msg, byte[] blinding);
        (byte[], byte[]) Split(byte[] blinding);
        string Stamp();
        CoinService Stamp(string stamp);
        TransactionCoin TransactionCoin();
        CoinService TransactionCoin(TransactionCoin transactionCoin);
        CoinDto SwapPartialOne(SecureString password, CoinDto coin, RedemptionKeyDto redemptionKey);
        int VerifyCoin(CoinDto terminal, CoinDto current);
        int Version();
        CoinService Version(int version);
    }
}