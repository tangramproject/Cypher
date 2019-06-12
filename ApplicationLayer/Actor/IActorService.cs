// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using TangramCypher.Helper.Http;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        event MessagePumpEventHandler MessagePump;
        Session GetSession(Guid sessionId);
        Client Client { get; }
        State State { get; }
        Task Tansfer(Session Session);
        Task ReceivePayment(Session session);
        Task<string> ReceivePaymentRedemptionKey(Session session, string cypher);

    }
}