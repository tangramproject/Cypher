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
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Coin
{
    public interface ICoinService
    {
        CoinService BuildReceiver(SecureString secret);
        CoinService BuildSender(SecureString secret);
        void ClearCache();
        (CoinDto, CoinDto) CoinSwap(SecureString secret, CoinDto coin, RedemptionKeyDto redemptionKey);
        CoinDto Coin();
        CoinDto DeriveCoin(CoinDto coin, SecureString secret);
        byte[] DeriveKey(ulong amount, string stamp, int version, SecureString secret);
        string DeriveKey(int version, string stamp, SecureString secret, int bytes = 32);
        string NewStamp();
        byte[] Hash(CoinDto coin);
        RedemptionKeyDto HotRelease(SecureString secret, string memo);
        void MakeSingleCoin(SecureString secret);
        ProofStruct ProofStruct();
        string PartialRelease(int version, string stamp, string memo, SecureString secret);
        byte[] Sign(ulong amount, int version, string stamp, SecureString secret, byte[] msg);
        byte[] SignWithBlinding(byte[] msg, byte[] blinding);
        (byte[], byte[]) Split(byte[] blinding, SecureString secret);
        string Stamp();
        CoinService Stamp(string stamp);
        TransactionCoinDto TransactionCoin();
        CoinService TransactionCoin(TransactionCoinDto transactionCoin);
        CoinDto SwapPartialOne(SecureString secret, CoinDto coin, RedemptionKeyDto redemptionKey);
        int VerifyCoin(CoinDto terminal, CoinDto current);
        int Version();
        CoinService Version(int version);
    }
}