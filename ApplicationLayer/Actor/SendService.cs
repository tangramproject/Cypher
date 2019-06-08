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

                    if (lastError == null)
                        await machine.FireAsync(unlockTrigger, sessionId);
                    else
                        machine.Fire(Trigger.Failed);
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.Unlock, State.Keys)
                .Permit(Trigger.Failed, State.Failure);

            machine.Configure(State.Keys)
                .OnEntryFromAsync(unlockTrigger, async (Guid sessionId) =>
               {
                   await Unlock(sessionId);

                   if (lastError == null)
                       await machine.FireAsync(burnTrigger, sessionId);
                   else
                       machine.Fire(Trigger.Failed);
               })
                .PermitReentry(Trigger.Unlock)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Torch, State.Burned)
                .Permit(Trigger.Failed, State.Failure);

            machine.Configure(State.Burned)
                .OnEntryFromAsync(burnTrigger, async (Guid sessionId) =>
                {
                    await Burn(sessionId);

                    if (lastError == null)
                        await machine.FireAsync(commitReceiverTrigger, sessionId);
                    else
                        machine.Fire(Trigger.Failed);
                })
                .PermitReentry(Trigger.Torch)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Commit, State.Committed)
                .Permit(Trigger.Failed, State.Failure);

            machine.Configure(State.Committed)
                .OnEntryFromAsync(commitReceiverTrigger, async (Guid sessionId) =>
                {
                    await CommitReceiver(sessionId);
                    await machine.FireAsync(publicKeyAgreementTrgger, sessionId);
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.PublicKeyAgreement, State.PublicKeyAgree);

            machine.Configure(State.PublicKeyAgree)
                .OnEntryFromAsync(publicKeyAgreementTrgger, async (Guid sessionId) =>
                {
                    await PublicKeyAgreementMessage(sessionId);
                    await machine.FireAsync(redemptionKeyTrigger, sessionId);
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.PrepareRedemptionKey, State.RedemptionKey);

            machine.Configure(State.RedemptionKey)
                .OnEntryFromAsync(redemptionKeyTrigger, async (Guid sessionId) =>
                {
                    await RedemptionKeyMessage(sessionId);
                    await machine.FireAsync(paymentTrgger, sessionId);
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.PaymentAgreement, State.Payment);

            machine.Configure(State.Payment)
                .OnEntryFromAsync(paymentTrgger, async (Guid sessionId) =>
                {
                    UpdateMessagePump("Busy committing payment agreement ...");

                    var session = GetSession(sessionId);

                    //TODO.. 
                    var rece = await unitOfWork
                                .GetReceiverRepository()
                                .Get(session.Identifier, session.MasterKey, StoreKey.TransactionIdKey, session.SessionId.ToString());

                    var pub = await unitOfWork
                                .GetPublicKeyAgreementRepository()
                                .Get(session.Identifier, session.MasterKey, StoreKey.TransactionIdKey, session.SessionId.ToString());

                    var redem = await unitOfWork
                                    .GetRedemptionRepository()
                                    .Get(session.Identifier, session.MasterKey, StoreKey.TransactionIdKey, session.SessionId.ToString());

                    var coins = await PostParallel(new List<CoinDto>() { rece }, RestApiMethod.PostCoin);
                    var msgs = await PostParallel(new List<MessageDto> { pub, redem.Message }, RestApiMethod.PostMessage);

                    machine.Fire(Trigger.Complete);
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.Complete, State.Completed);

            machine.Configure(State.Completed)
                .Permit(Trigger.Verify, State.Audited);

            machine.Configure(State.Failure)
                .Permit(Trigger.Verify, State.Audited);
        }

        private void Configure(string stateString)
        {
            var state = (State)Enum.Parse(typeof(State), stateString);
            machine = new StateMachine<State, Trigger>(State);

            Configure();
        }
    }
}