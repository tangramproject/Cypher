// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Security;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Coin
{
    public interface ICoinService
    {
        TaskResult<bool> Receiver(SecureString secret, ulong input, out ReceiverCoinDto coin, out byte[] blind, out byte[] salt);
        TaskResult<SenderCoinDto> Sender(Session session, PurchaseDto purchase);
        (ICoinDto, ICoinDto) CoinSwap(SecureString secret, SecureString salt, ICoinDto coin, RedemptionKeyDto redemptionKey);
        ICoinDto DeriveCoin(ICoinDto coin, SecureString secret, SecureString salt);
        byte[] DeriveKey(ulong amount, string stamp, int version, SecureString secret, SecureString salt);
        string DeriveKey(int version, string stamp, SecureString secret, SecureString salt, int bytes = 32);
        byte[] Hash(ICoinDto coin);
        (string, string) HotRelease(int version, string stamp, SecureString secret, SecureString salt);
        ICoinDto MakeSingleCoin(SecureString secret, SecureString salt, string stamp, int version);
        string PartialRelease(int version, string stamp, string memo, SecureString secret, SecureString salt);
        byte[] Sign(ulong amount, int version, string stamp, SecureString secret, SecureString salt, byte[] msg);
        byte[] SignWithBlinding(byte[] msg, byte[] blinding);
        ICoinDto SwapPartialOne(SecureString secret, SecureString salt, ICoinDto coin, RedemptionKeyDto redemptionKey);
        int VerifyCoin(ICoinDto terminal, ICoinDto current);
    }
}