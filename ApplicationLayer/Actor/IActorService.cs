// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Coin;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        Task<MessageDto> AddMessageAsync(MessageDto message, CancellationToken cancellationToken);
        Task<CoinDto> AddCoinAsync(CoinDto coin, CancellationToken cancellationToken);
        double? Amount();
        ActorService Amount(double? value);
        double? GetChange();
        Task<double> CheckBalance();
        Span<byte> DecodeAddress(string key);
        SecureString From();
        ActorService From(SecureString password);
        byte[] Cypher(string message, byte[] pk);
        Task<NotificationDto> GetMessageAsync(string address, CancellationToken cancellationToken);
        Task<IEnumerable<NotificationDto>> GetMessagesAsync(string address, int skip, int take, CancellationToken cancellationToken);
        Task<byte[]> ToSharedKey(byte[] pk);
        Task<CoinDto> GetCoinAsync(string stamp, CancellationToken cancellationToken);
        SecureString Identifier();
        ActorService Identifier(SecureString walletId);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cypher, PkSkDto pkSkDto);
        SecureString PublicKey();
        ActorService PublicKey(SecureString pk);
        Task ReceivePayment(string address, NotificationDto notification);
        SecureString SecretKey();
        ActorService SecretKey(SecureString sk);
        Task<MessageDto> EstablishPubKeyMessage();
        Task<JObject> SendPayment(bool sendMessage);
        string To();
        ActorService To(string address);
        string ProverPassword(SecureString password, int version);
    }
}