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
            await machine.FireAsync<Guid>(verifyTrigger, session.SessionId);
        }

        private async Task TransferPublicKeyAgreement(Guid sessionId) => await machine.FireAsync(publicKeyAgreementTrgger, sessionId);

        private async Task TransferPaymentAgreement(Guid sessionId) => await machine.FireAsync(paymentTrgger, sessionId);

        private void Configure()
        {
            machine.Configure(State.New)
                   .Permit(Trigger.Verify, State.Audited);

            machine.Configure(State.Audited)
                .SubstateOf(State.New)
                .OnEntryFromAsync(verifyTrigger, async (Guid sessionId) =>
                {
                    await SufficientFunds(sessionId);
                    await machine.FireAsync(Trigger.Unlock);
                })
                .PermitReentry(Trigger.Verify)
                .PermitIf(Trigger.Unlock, State.Keys, () => true);

            machine.Configure(State.Keys)
                .OnEntryFromAsync(unlockTrigger, async (Guid sessionId) =>
               {
                   await Unlock(sessionId);
                   await machine.FireAsync(Trigger.Torch);
               })
                .Permit(Trigger.Torch, State.Burned);

            machine.Configure(State.Burned)
                .OnEntryFromAsync(burnTrigger, async (Guid sessionId) =>
                {
                    await Burn(sessionId);
                    await machine.FireAsync(Trigger.Commit);
                })
                .PermitIf(Trigger.Commit, State.Committed, () => true);

            machine.Configure(State.Committed)
                .OnEntryFromAsync(commitReceiverTrigger, async (Guid sessionId) =>
                {
                    await CommitReceiver(sessionId);
                    await machine.FireAsync(Trigger.PrepareRedemptionKey);
                })
                .PermitIf(Trigger.PrepareRedemptionKey, State.RedemptionKey, () => true);

            machine.Configure(State.RedemptionKey)
                .OnEntryFromAsync(redemptionKeyTrigger, async (Guid sessionId) =>
                {
                    RedemptionKeyMessage(sessionId);

                    var session = GetSession(sessionId);
                    var added = await unitOfWork
                                        .GetRedemptionRepository()
                                        .Put(session.Identifier, session.MasterKey, StoreKey.HashKey, session.MessageStore.Hash, session.MessageStore);
                })
                .PermitIf(Trigger.PublicKeyAgreement, State.PublicKeyAgree, () => true);

            machine.Configure(State.PublicKeyAgree)
                .OnEntryFromAsync(publicKeyAgreementTrgger, async (Guid sessionId) =>
                {
                    await PublicKeyAgreementMessage(sessionId);
                })
                .PermitIf(Trigger.PaymentAgreement, State.Payment, () => true);

            machine.Configure(State.Payment)
                .OnEntryFromAsync(paymentTrgger, async (Guid sessionId) =>
                {
                    var session = GetSession(sessionId);
                    session.PaymentAgreementMessage = await Util.TriesUntilCompleted<MessageDto>(
                                    async () => { return await AddAsync(session.MessageStore.Message, RestApiMethod.PostMessage); }, 10, 100);
                    SessionAddOrUpdate(session);
                })
                .Permit(Trigger.Complete, State.Completed);
        }

        private void Configure(string stateString)
        {
            var state = (State)Enum.Parse(typeof(State), stateString);
            machine = new StateMachine<State, Trigger>(State);

            Configure();
        }
    }
}