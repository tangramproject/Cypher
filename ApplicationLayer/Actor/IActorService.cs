// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TangramCypher.Helper;
using TangramCypher.Helper.Http;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        event MessagePumpEventHandler MessagePump;

        Session GetSession(Guid sessionId);
        Client GetClient(IConfiguration configuration);

        Task ReceivePayment(Session session);
        Task<TaskResult<bool>> ReceivePaymentRedemptionKey(Session session, string cypher);
        Session SessionAddOrUpdate(Session session);
        TaskResult<Session> SufficientFunds(Guid sessionId);
        TaskResult<bool> Unlock(Guid sessionId);
        TaskResult<bool> Burn(Guid sessionId);
        TaskResult<bool> CommitReceiver(Guid sessionId);
        TaskResult<bool> PublicKeyAgreementMessage(Guid sessionId);
        TaskResult<bool> RedemptionKeyMessage(Guid sessionId);
        Task<TaskResult<byte[]>> PostArticle<T>(T payload, RestApiMethod api) where T : class;
        void UpdateMessagePump(string message);
    }
}