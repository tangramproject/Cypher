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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Coin;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        event MessagePumpEventHandler MessagePump;

        Task<T> AddAsync<T>(T payload, RestApiMethod apiMethod);
        ulong Amount();
        ActorService Amount(double value);
        Task<T> GetAsync<T>(string address, RestApiMethod apiMethod);
        Task<ulong> CheckBalance();
        JObject GetLastError();
        Task<IEnumerable<T>> GetRangeAsync<T>(string address, int skip, int take, RestApiMethod apiMethod);
        Span<byte> DecodeAddress(string key);
        SecureString MasterKey();
        ActorService MasterKey(SecureString password);
        byte[] Cypher(string message, byte[] pk);
        byte[] ToSharedKey(byte[] pk);
        SecureString Identifier();
        ActorService Identifier(SecureString walletId);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cypher, PkSkDto pkSkDto);
        SecureString PublicKey();
        ActorService PublicKey(SecureString pk);
        Task ReceivePayment();
        Task<JObject> ReceivePaymentRedemptionKey(string cypher);
        SecureString SecretKey();
        ActorService SecretKey(SecureString sk);
        Task<MessageDto> EstablishPubKeyMessage();
        Task<bool> SendPayment();
        Task<JObject> SendPaymentMessage(bool send);
        string ToAddress();
        ActorService ToAddress(string address);
        Task<List<TransactionDto>> Sync();
        string FromAddress();
        ActorService FromAddress(string address);
        Task<bool> Payment(RedemptionKeyDto redemptionKey, CoinDto coin);
        Task SetRandomAddress();
        Task SetSecretKey();
        Task SetPublicKey();
    }
}