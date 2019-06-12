// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
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

            ConfigureStateAudited();
            ConfigureStateKeys();
            ConfigureStateBurned();
            ConfigureStateCommitted();
            ConfigureStatePublicKeyAgree();
            ConfigureStatRedeptionKey();
            ConfigureStatePayment();

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

        private void ConfigureStateAudited() => machine.Configure(State.Audited)
                .SubstateOf(State.New)
                .OnEntryFromAsync(verifyTrigger, async (Guid sessionId) =>
                {
                    var funds = await SufficientFunds(sessionId);
                    if (funds.Success)
                        await machine.FireAsync(unlockTrigger, funds.Result.SessionId);
                    else
                    {
                        var session = GetSession(sessionId);
                        session.LastError = funds.NonSuccessMessage;
                        SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.Unlock, State.Keys)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStateKeys() => machine.Configure(State.Keys)
                .OnEntryFromAsync(unlockTrigger, async (Guid sessionId) =>
                {
                    var unlocked = await Unlock(sessionId);
                    if (unlocked.Success)
                        await machine.FireAsync(burnTrigger, sessionId);
                    else
                    {
                        var session = GetSession(sessionId);
                        session.LastError = unlocked.NonSuccessMessage;
                        SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Unlock)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Torch, State.Burned)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStateBurned() => machine.Configure(State.Burned)
                .OnEntryFromAsync(burnTrigger, async (Guid sessionId) =>
                {
                    var burnt = await Burn(sessionId);
                    if (burnt.Success)
                        await machine.FireAsync(commitReceiverTrigger, sessionId);
                    else
                    {
                        var session = GetSession(sessionId);
                        session.LastError = burnt.NonSuccessMessage;
                        SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Torch)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Commit, State.Committed)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStateCommitted() => machine.Configure(State.Committed)
                  .OnEntryFromAsync(commitReceiverTrigger, async (Guid sessionId) =>
                  {
                      var committed = await CommitReceiver(sessionId);
                      if (committed.Success)
                          await machine.FireAsync(publicKeyAgreementTrgger, sessionId);
                      else
                      {
                          var session = GetSession(sessionId);
                          session.LastError = committed.NonSuccessMessage;
                          SessionAddOrUpdate(session);
                          machine.Fire(Trigger.Failed);
                      }
                  })
                  .PermitReentry(Trigger.Commit)
                  .Permit(Trigger.Verify, State.Audited)
                  .Permit(Trigger.PublicKeyAgreement, State.PublicKeyAgree)
                  .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStatePublicKeyAgree() => machine.Configure(State.PublicKeyAgree)
                .OnEntryFromAsync(publicKeyAgreementTrgger, async (Guid sessionId) =>
                {
                    var pubAgreed = await PublicKeyAgreementMessage(sessionId);
                    if (pubAgreed.Success)
                        await machine.FireAsync(redemptionKeyTrigger, sessionId);
                    else
                    {
                        var session = GetSession(sessionId);
                        session.LastError = pubAgreed.NonSuccessMessage;
                        SessionAddOrUpdate(session);
                        machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.PublicKeyAgreement)
                .Permit(Trigger.PrepareRedemptionKey, State.RedemptionKey)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure);

        private void ConfigureStatRedeptionKey() => machine.Configure(State.RedemptionKey)
                .OnEntryFromAsync(redemptionKeyTrigger, async (Guid sessionId) =>
                {
                    var redeemed = await RedemptionKeyMessage(sessionId);
                    if (redeemed.Success)
                        await machine.FireAsync(paymentTrgger, sessionId);
                    else
                    {
                        var session = GetSession(sessionId);
                        session.LastError = redeemed.NonSuccessMessage;
                        SessionAddOrUpdate(session);
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
                    UpdateMessagePump("Busy committing payment agreement ...");

                    var session = GetSession(sessionId);
                    var que = new QueueDto() { DateTime = DateTime.Now, TransactionId = session.SessionId };
                    var storeKey = StoreKey.TransactionIdKey;
                    var txnId = session.SessionId.ToString();

                    try
                    {
                        var send = await unitOfWork.GetSenderRepository().Get(session, storeKey, txnId);
                        var sendResult = await PostArticle(send.Result, RestApiMethod.PostCoin);
                        if (sendResult.Result == null)
                            throw new NullReferenceException("Sender failed to post the request!");

                        var rece = await unitOfWork.GetReceiverRepository().Get(session, storeKey, txnId);
                        var receResult = await PostArticle(rece, RestApiMethod.PostCoin);
                        if (receResult.Result == null)
                            que.ReceiverFailed = true;

                        var publ = await unitOfWork.GetPublicKeyAgreementRepository().Get(session, storeKey, txnId);
                        var publResult = await PostArticle(publ, RestApiMethod.PostMessage);
                        if (publ.Result == null)
                            que.PublicAgreementFailed = true;

                        var rede = await unitOfWork.GetRedemptionRepository().Get(session, storeKey, txnId);
                        var redeResult = await PostArticle(rede, RestApiMethod.PostMessage);
                        if (redeResult.Result == null)
                            que.PaymentFailed = true;

                        var checkList = new List<bool> { que.PaymentFailed, que.PublicAgreementFailed, que.ReceiverFailed };
                        if (checkList.Any(l => l.Equals(true)))
                        {
                            var addQueue = await unitOfWork
                                                .GetQueueRepository()
                                                .Put(session, StoreKey.TransactionIdKey, que.TransactionId.ToString(), que);

                            if (addQueue.Success.Equals(false))
                            {
                                throw new Exception("Queue failed to save..");
                            }
                        }

                        machine.Fire(Trigger.Complete);
                    }
                    catch (Exception ex)
                    {
                        session = GetSession(sessionId);
                        session.LastError = JObject.FromObject(new
                        {
                            success = false,
                            message = ex.Message
                        });
                        SessionAddOrUpdate(session);
                        await machine.FireAsync(Trigger.RollBack);
                    }
                })
                .PermitReentry(Trigger.PaymentAgreement)
                .Permit(Trigger.Complete, State.Completed)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure)
                .Permit(Trigger.RollBack, State.Reversed);

        private void ConfigureStateReversed() => machine.Configure(State.Reversed)
                .OnEntryFromAsync(reversedTrgger, async (Guid sessionId) =>
                {
                    var session = GetSession(sessionId);
                    var send = await unitOfWork
                                .GetSenderRepository()
                                .Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

                    if (send.Success)
                    {
                        var deleted = await unitOfWork
                                        .GetTransactionRepository()
                                        .Delete(session, StoreKey.HashKey, send.Result.Hash);
                    }

                    machine.Fire(Trigger.Failed);
                })
                .PermitReentry(Trigger.RollBack)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure);
    }
}