// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stateless;
using Stateless.Graph;
using TangramCypher.Helper;
using TangramCypher.Model;
using State = TangramCypher.ApplicationLayer.Actor.State;

namespace TangramCypher.ApplicationLayer.Actor
{
    public partial class ActorService
    {
        public State State => machine.State;

        public string Graph() => UmlDotGraph.Format(machine.GetInfo());

        public async Task Tansfer(Session session)
        {
            session = SessionAddOrUpdate(session);

            try
            {
                await machine.FireAsync<Guid>(verifyTrigger, session.SessionId);
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }
        }

        private void Configure()
        {
            machine.Configure(State.New)
                   .Permit(Trigger.Verify, State.Audited);

            machine.Configure(State.Audited)
                .SubstateOf(State.New)
                .OnEntryFromAsync(verifyTrigger, async (Guid sessionId) =>
                {
                    await SufficientFunds(sessionId);
                    await machine.FireAsync(unlockTrigger, sessionId);
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.Unlock, State.Keys);

            machine.Configure(State.Keys)
                .OnEntryFromAsync(unlockTrigger, async (Guid sessionId) =>
               {
                   await Unlock(sessionId);
                   await machine.FireAsync(burnTrigger, sessionId);
               })
                .Permit(Trigger.Torch, State.Burned);

            machine.Configure(State.Burned)
                .OnEntryFromAsync(burnTrigger, async (Guid sessionId) =>
                {
                    await Burn(sessionId);
                    await machine.FireAsync(commitReceiverTrigger, sessionId);
                })
                .Permit(Trigger.Commit, State.Committed);

            machine.Configure(State.Committed)
                .OnEntryFromAsync(commitReceiverTrigger, async (Guid sessionId) =>
                {
                    await CommitReceiver(sessionId);
                    await machine.FireAsync(redemptionKeyTrigger, sessionId);
                })
                .Permit(Trigger.PrepareRedemptionKey, State.RedemptionKey);

            machine.Configure(State.RedemptionKey)
                .OnEntryFromAsync(redemptionKeyTrigger, async (Guid sessionId) =>
                {
                    RedemptionKeyMessage(sessionId);

                    var session = GetSession(sessionId);
                    var added = await unitOfWork
                                        .GetRedemptionRepository()
                                        .Put(session.Identifier, session.MasterKey, StoreKey.HashKey, session.MessageStore.Hash, session.MessageStore);
                    await machine.FireAsync(publicKeyAgreementTrgger, sessionId);
                })
                .Permit(Trigger.PublicKeyAgreement, State.PublicKeyAgree);

            machine.Configure(State.PublicKeyAgree)
                .OnEntryFromAsync(publicKeyAgreementTrgger, async (Guid sessionId) =>
                {
                    await PublicKeyAgreementMessage(sessionId);
                    await machine.FireAsync(paymentTrgger, sessionId);
                })
                .Permit(Trigger.PaymentAgreement, State.Payment);

            machine.Configure(State.Payment)
                .OnEntryFromAsync(paymentTrgger, async (Guid sessionId) =>
                {
                    UpdateMessagePump("Busy committing payment agreement ...");

                    var session = GetSession(sessionId);

                    session.PaymentAgreementMessage = await Util.TriesUntilCompleted<MessageDto>(
                                    async () => { return await AddAsync(session.MessageStore.Message, RestApiMethod.PostMessage); }, 10, 100);

                    SessionAddOrUpdate(session);
                    machine.Fire(Trigger.Complete);
                })
                .Permit(Trigger.Complete, State.Completed);

            machine.Configure(State.Completed).PermitReentry(Trigger.Verify);
        }

        private void Configure(string stateString)
        {
            var state = (State)Enum.Parse(typeof(State), stateString);
            machine = new StateMachine<State, Trigger>(State);

            Configure();
        }
    }
}