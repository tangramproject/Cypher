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
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        event MessagePumpEventHandler MessagePump;
        Task<T> AddAsync<T>(T payload, RestApiMethod apiMethod);
        Task<T> GetAsync<T>(string address, RestApiMethod apiMethod);
        JObject GetLastError();
        Session GetSession(Guid sessionId);
        Task<IEnumerable<T>> GetRangeAsync<T>(string address, int skip, int take, RestApiMethod apiMethod);
        State State { get; }
        Task Tansfer(Session Session);
        Task ReceivePayment(Session session);
        Task<JObject> ReceivePaymentRedemptionKey(Session session, string cypher);

    }
}