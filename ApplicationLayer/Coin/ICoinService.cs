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
using Secp256k1_ZKP.Net;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Coin
{
    public interface ICoinService
    {
        (CoinDto, byte[]) Receiver(SecureString secret, ulong input);
        Task<CoinDto> Sender(SecureString identifier, SecureString secret, PurchaseDto purchase);
        (CoinDto, CoinDto) CoinSwap(SecureString secret, CoinDto coin, RedemptionKeyDto redemptionKey);
        CoinDto DeriveCoin(CoinDto coin, SecureString secret);
        byte[] DeriveKey(ulong amount, string stamp, int version, SecureString secret);
        string DeriveKey(int version, string stamp, SecureString secret, int bytes = 32);
        byte[] Hash(CoinDto coin);
        (string, string) HotRelease(int version, string stamp, SecureString secret);
        CoinDto MakeSingleCoin(SecureString secret, string stamp, int version);
        string PartialRelease(int version, string stamp, string memo, SecureString secret);
        byte[] Sign(ulong amount, int version, string stamp, SecureString secret, byte[] msg);
        byte[] SignWithBlinding(byte[] msg, byte[] blinding);
        CoinDto SwapPartialOne(SecureString secret, CoinDto coin, RedemptionKeyDto redemptionKey);
        int VerifyCoin(CoinDto terminal, CoinDto current);
    }
}