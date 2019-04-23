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
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        event MessagePumpEventHandler MessagePump;

        Task<T> AddAsync<T>(T payload, RestApiMethod apiMethod);
        double Amount();
        ActorService Amount(double value);
        Task<T> GetAsync<T>(string address, RestApiMethod apiMethod);
        Task<double> CheckBalance();
        double GetChange();
        JObject GetLastError();
        Task<IEnumerable<T>> GetRangeAsync<T>(string address, int skip, int take, RestApiMethod apiMethod);
        Span<byte> DecodeAddress(string key);
        SecureString From();
        ActorService From(SecureString password);
        byte[] Cypher(string message, byte[] pk);
        Task<byte[]> ToSharedKey(byte[] pk);
        SecureString Identifier();
        ActorService Identifier(SecureString walletId);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cypher, PkSkDto pkSkDto);
        SecureString PublicKey();
        ActorService PublicKey(SecureString pk);
        Task ReceivePayment(string address, bool sharedKey = false, byte[] receiverPk = null);
        Task<JObject> ReceivePaymentRedemptionKey(string address, string cypher);
        SecureString SecretKey();
        ActorService SecretKey(SecureString sk);
        Task<MessageDto> EstablishPubKeyMessage();
        Task<bool> SendPayment();
        Task<JObject> SendPaymentMessage(bool send);
        string To();
        ActorService To(string address);
        Task<List<TransactionDto>> Sync();
    }
}