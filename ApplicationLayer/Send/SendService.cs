// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Stateless;
using Stateless.Graph;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.Model;
using State = TangramCypher.ApplicationLayer.Actor.State;

namespace TangramCypher.ApplicationLayer.Send
{
    public class SendService : ISendService
    {
        private readonly IActorService actorService;
        private readonly ILogger logger;
        private StateMachine<State, Trigger> machine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> verifyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> unlockTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> burnTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> commitReceiverTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> redemptionKeyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> publicKeyAgreementTrgger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> paymentTrgger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> reversedTrgger;

        public SendService(IActorService actorService, ILogger logger)
        {
            this.actorService = actorService;
            this.logger = logger;

            machine = new StateMachine<State, Trigger>(State.New);

            verifyTrigger = machine.SetTriggerParameters<Guid>(Trigger.Verify);
            unlockTrigger = machine.SetTriggerParameters<Guid>(Trigger.Unlock);
            burnTrigger = machine.SetTriggerParameters<Guid>(Trigger.Torch);
            commitReceiverTrigger = machine.SetTriggerParameters<Guid>(Trigger.Commit);
            redemptionKeyTrigger = machine.SetTriggerParameters<Guid>(Trigger.PrepareRedemptionKey);
            publicKeyAgreementTrgger = machine.SetTriggerParameters<Guid>(Trigger.PublicKeyAgreement);
            paymentTrgger = machine.SetTriggerParameters<Guid>(Trigger.PaymentAgreement);
            reversedTrgger = machine.SetTriggerParameters<Guid>(Trigger.RollBack);

            ConfigureStateMachine();
        }

        public string Graph() => UmlDotGraph.Format(machine.GetInfo());

        public State State => machine.State;

        public async Task Tansfer(Session session)
        {
            session = actorService.SessionAddOrUpdate(session);

            try
            {
                await machine.FireAsync(verifyTrigger, session.SessionId);
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }
        }

        private void ConfigureStateMachine()
        {
            machine.Configure(State.New)
                   .Permit(Trigger.Verify, State.Audited);

            ConfigureStateAudited();
            ConfigureStateKeys();
            ConfigureStateBurned();
            ConfigureStateCommitted();
            ConfigureStatePublicKeyAgree();
            ConfigureStatRedeptionKey();
            ConfigureStatePayment();
            ConfigureStateReversed();

            machine.Configure(State.Completed)
                .Permit(Trigger.Verify, State.Audited);

            machine.Configure(State.Failure)
                .Permit(Trigger.Verify, State.Audited);
        }

        private void Configure(string stateString)
        {
            _ = (State)Enum.Parse(typeof(State), stateString);
            machine = new StateMachine<State, Trigger>(State);

            ConfigureStateMachine();
        }

        private void ConfigureStateAudited() => machine.Configure(State.Audited)
                .SubstateOf(State.New)
                .OnEntryFrom(verifyTrigger, (Guid sessionId) =>
                {
                    var funds = actorService.SufficientFunds(sessionId);
                    if (funds.Success)
                        machine.Fire(unlockTrigger, funds.Result.SessionId);
                    else
                    {
                        var session = actorService.GetSession(sessionId);
                        session.LastError = funds.NonSuccessMessage;
                        actorService.SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.Unlock, State.Keys)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStateKeys() => machine.Configure(State.Keys)
                .OnEntryFrom(unlockTrigger, (Guid sessionId) =>
                {
                    var unlocked = actorService.Unlock(sessionId);
                    if (unlocked.Success)
                        machine.Fire(burnTrigger, sessionId);
                    else
                    {
                        var session = actorService.GetSession(sessionId);
                        session.LastError = unlocked.NonSuccessMessage;
                        actorService.SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Unlock)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Torch, State.Burned)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStateBurned() => machine.Configure(State.Burned)
                .OnEntryFrom(burnTrigger, (Guid sessionId) =>
                {
                    var burnt = actorService.Burn(sessionId);
                    if (burnt.Success)
                        machine.Fire(commitReceiverTrigger, sessionId);
                    else
                    {
                        var session = actorService.GetSession(sessionId);
                        session.LastError = burnt.NonSuccessMessage;
                        actorService.SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Torch)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Commit, State.Committed)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStateCommitted() => machine.Configure(State.Committed)
                  .OnEntryFrom(commitReceiverTrigger, (Guid sessionId) =>
                  {
                      var committed = actorService.CommitReceiver(sessionId);
                      if (committed.Success)
                          machine.Fire(publicKeyAgreementTrgger, sessionId);
                      else
                      {
                          var session = actorService.GetSession(sessionId);
                          session.LastError = committed.NonSuccessMessage;
                          actorService.SessionAddOrUpdate(session);
                          machine.Fire(Trigger.Failed);
                      }
                  })
                  .PermitReentry(Trigger.Commit)
                  .Permit(Trigger.Verify, State.Audited)
                  .Permit(Trigger.PublicKeyAgreement, State.PublicKeyAgree)
                  .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStatePublicKeyAgree() => machine.Configure(State.PublicKeyAgree)
                .OnEntryFrom(publicKeyAgreementTrgger, (Guid sessionId) =>
                {
                    var pubAgreed = actorService.PublicKeyAgreementMessage(sessionId);
                    if (pubAgreed.Success)
                        machine.Fire(redemptionKeyTrigger, sessionId);
                    else
                    {
                        var session = actorService.GetSession(sessionId);
                        session.LastError = pubAgreed.NonSuccessMessage;
                        actorService.SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.PublicKeyAgreement)
                .Permit(Trigger.PrepareRedemptionKey, State.RedemptionKey)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStatRedeptionKey() => machine.Configure(State.RedemptionKey)
                .OnEntryFrom(redemptionKeyTrigger, (Guid sessionId) =>
                {
                    var redeemed = actorService.RedemptionKeyMessage(sessionId);
                    if (redeemed.Success)
                        machine.Fire(paymentTrgger, sessionId);
                    else
                    {
                        var session = actorService.GetSession(sessionId);
                        session.LastError = redeemed.NonSuccessMessage;
                        actorService.SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.PrepareRedemptionKey)
                .Permit(Trigger.PaymentAgreement, State.Payment)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStatePayment() => machine.Configure(State.Payment)
                .OnEntryFromAsync(paymentTrgger, async (Guid sessionId) =>
                {
                    actorService.UpdateMessagePump("Busy committing payment agreement ...");

                    var session = actorService.GetSession(sessionId);
                    var que = new QueueDto { DateTime = DateTime.Now, TransactionId = session.SessionId };
                    var storeKey = StoreKey.TransactionIdKey;
                    var txnId = session.SessionId.ToString();

                    try
                    {
                        using (var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString()))
                        {
                            var sender = db.Query<SenderCoinDto>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();
                            var sendResult = await actorService.PostArticle(sender.Cast<BaseCoinDto>(), RestApiMethod.PostCoin);
                            if (sendResult.Result == null)
                            {
                                throw new NullReferenceException("Sender failed to post the request!");
                            }

                            var receiver = db.Query<ReceiverCoinDto>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();
                            var receResult = await actorService.PostArticle(receiver.Cast<BaseCoinDto>(), RestApiMethod.PostCoin);
                            if (receResult.Result == null)
                            {
                                que.ReceiverFailed = true;
                            }

                            if (session.ForwardMessage)
                            {
                                var publicKeyAgreement = db.Query<MessageDto>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();
                                var publResult = await actorService.PostArticle(publicKeyAgreement, RestApiMethod.PostMessage);
                                if (publResult.Result == null)
                                {
                                    que.PublicAgreementFailed = true;
                                }

                                var redemptionKey = db.Query<MessageStoreDto>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();
                                var redeResult = await actorService.PostArticle(redemptionKey.Message, RestApiMethod.PostMessage);
                                if (redeResult.Result == null)
                                {
                                    que.PaymentFailed = true;
                                }
                            }

                            var checkList = new List<bool> { que.PaymentFailed, que.PublicAgreementFailed, que.ReceiverFailed };
                            if (checkList.Any(l => l.Equals(true)))
                            {
                                db.Insert(que);
                                logger.LogInformation("Added queue.. you might have to do some manual work ;(.. WIP");
                            }
                            else
                            {
                                db.Delete<SenderCoinDto>(session.SessionId);
                                db.Delete<ReceiverCoinDto>(session.SessionId);
                                db.Delete<MessageDto>(session.SessionId);
                                db.Delete<MessageStoreDto>(session.SessionId);
                            }

                            machine.Fire(Trigger.Complete);
                        }
                    }
                    catch (Exception ex)
                    {
                        session = actorService.GetSession(sessionId);
                        session.LastError = JObject.FromObject(new
                        {
                            success = false,
                            message = ex.Message
                        });
                        actorService.SessionAddOrUpdate(session);
                        logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        await machine.FireAsync(reversedTrgger, session.SessionId);
                    }
                })
                .PermitReentry(Trigger.PaymentAgreement)
                .Permit(Trigger.Complete, State.Completed)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure)
                .Permit(Trigger.RollBack, State.Reversed);

        private void ConfigureStateReversed() => machine.Configure(State.Reversed)
                .OnEntryFrom(reversedTrgger, (Guid sessionId) =>
                {
                    var session = actorService.GetSession(sessionId);
                    using (var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString()))
                    {
                        var senderExists = db.Query<SenderCoinDto>().Where(s => s.TransactionId.Equals(session.SessionId)).Exists();
                        if (senderExists.Equals(true))
                        {
                            var sender = db.Query<SenderCoinDto>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();
                            var success = db.Delete<SenderCoinDto>(sender.TransactionId);
                            if (success.Equals(false))
                            {
                                var message = $"Please check the logs for any details. Could not delete sender transaction {sender.Hash}";

                                logger.LogError(message);
                                throw new Exception(message);
                            }
                        }
                    }

                    machine.Fire(Trigger.Failed);
                })
                .PermitReentry(Trigger.RollBack)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure);

    }
}
