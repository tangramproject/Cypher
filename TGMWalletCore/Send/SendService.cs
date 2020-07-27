// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Stateless;
using Stateless.Graph;
using TGMWalletCore.Actor;
using TGMWalletCore.Helper;
using TGMWalletCore.Model;
using State = TGMWalletCore.Actor.State;

namespace TGMWalletCore.Send
{
    public class SendService : ISendService
    {
        private readonly IActorService _actorService;
        private readonly ILogger _logger;
        private StateMachine<State, Trigger> _machine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _verifyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _unlockTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _spendTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _paymentTrgger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _reversedTrgger;

        public SendService(IActorService actorService, ILogger<SendService> logger)
        {
            _actorService = actorService;
            _logger = logger;
            _machine = new StateMachine<State, Trigger>(State.New);
            _verifyTrigger = _machine.SetTriggerParameters<Guid>(Trigger.Verify);
            _unlockTrigger = _machine.SetTriggerParameters<Guid>(Trigger.Unlock);
            _spendTrigger = _machine.SetTriggerParameters<Guid>(Trigger.Torch);
            _paymentTrgger = _machine.SetTriggerParameters<Guid>(Trigger.PaymentAgreement);
            _reversedTrgger = _machine.SetTriggerParameters<Guid>(Trigger.RollBack);

            ConfigureStateMachine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Graph() => UmlDotGraph.Format(_machine.GetInfo());

        /// <summary>
        /// 
        /// </summary>
        public State State => _machine.State;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public async Task Tansfer(Session session)
        {
            session = _actorService.SessionAddOrUpdate(session);

            try
            {
                await _machine.FireAsync(_verifyTrigger, session.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureStateMachine()
        {
            _machine.Configure(State.New)
                   .Permit(Trigger.Verify, State.Audited);

            ConfigureStateAudited();
            ConfigureStateKeys();
            ConfigureStateBurned();
            ConfigureStatePayment();
            ConfigureStateReversed();

            _machine.Configure(State.Completed)
                .Permit(Trigger.Verify, State.Audited);

            _machine.Configure(State.Failure)
                .Permit(Trigger.Verify, State.Audited);
        }

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureStateAudited() => _machine.Configure(State.Audited)
                .SubstateOf(State.New)
                .OnEntryFrom(_verifyTrigger, (Guid sessionId) =>
                {
                    var funds = _actorService.SufficientFunds(sessionId);
                    if (funds.Success)
                        _machine.Fire(_unlockTrigger, funds.Result.SessionId);
                    else
                    {
                        var session = _actorService.GetSession(sessionId);
                        session.LastError = funds.NonSuccessMessage;
                        _actorService.SessionAddOrUpdate(session);
                        _machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Verify)
                .Permit(Trigger.Unlock, State.Keys)
                .Permit(Trigger.Failed, State.Failure);

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureStateKeys() => _machine.Configure(State.Keys)
                .OnEntryFrom(_unlockTrigger, (Guid sessionId) =>
                {
                    var unlocked = _actorService.Unlock(sessionId);
                    if (unlocked.Success)
                        _machine.Fire(_spendTrigger, sessionId);
                    else
                    {
                        var session = _actorService.GetSession(sessionId);
                        session.LastError = unlocked.NonSuccessMessage;
                        _actorService.SessionAddOrUpdate(session);
                        _machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Unlock)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Torch, State.Burned)
                .Permit(Trigger.Failed, State.Failure);

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureStateBurned() => _machine.Configure(State.Burned)
                .OnEntryFrom(_spendTrigger, (Guid sessionId) =>
                {
                    var burnt = _actorService.Spend(sessionId);
                    if (burnt.Success)
                        _machine.Fire(_paymentTrgger, sessionId);
                    else
                    {
                        var session = _actorService.GetSession(sessionId);
                        session.LastError = burnt.NonSuccessMessage;
                        _actorService.SessionAddOrUpdate(session);
                        _machine.Fire(Trigger.Failed);
                    }
                })
                .PermitReentry(Trigger.Torch)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Commit, State.Committed)
                .Permit(Trigger.Failed, State.Failure);

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureStatePayment() => _machine.Configure(State.Payment)
                .OnEntryFromAsync(_paymentTrgger, async (Guid sessionId) =>
                {
                    var session = _actorService.GetSession(sessionId);
                    var que = new Queue { DateTime = DateTime.Now, TransactionId = session.SessionId };
                    var storeKey = StoreKey.TransactionIdKey;
                    var txnId = session.SessionId.ToString();

                    try
                    {
                        using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                        var sender = db.Query<Model.Coin>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();

                        _actorService.UpdateMessagePump("Sending your transaction ...");

                        var sendResult = await _actorService.PostArticle(sender, RestApiMethod.PostCoin);
                        if (sendResult.Result == null)
                        {
                            throw new NullReferenceException("Sender failed to post the request!");
                        }

                        var checkList = new List<bool> { que.PaymentFailed, que.PublicAgreementFailed, que.ReceiverFailed };
                        if (checkList.Any(l => l.Equals(true)))
                        {
                            db.Insert(que);
                            _logger.LogInformation("Added queue.. you might have to do some manual work ;(.. WIP");
                        }
                        else
                        {
                            db.Delete<Model.Coin>(session.SessionId);
                            db.Delete<Message>(session.SessionId);
                            db.Delete<MessageStore>(session.SessionId);
                        }

                        _machine.Fire(Trigger.Complete);
                    }
                    catch (Exception ex)
                    {
                        session = _actorService.GetSession(sessionId);
                        session.LastError = JObject.FromObject(new
                        {
                            success = false,
                            message = ex.Message
                        });
                        _actorService.SessionAddOrUpdate(session);
                        _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        await _machine.FireAsync(_reversedTrgger, session.SessionId);
                    }
                })
                .PermitReentry(Trigger.PaymentAgreement)
                .Permit(Trigger.Complete, State.Completed)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure)
                .Permit(Trigger.RollBack, State.Reversed);

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureStateReversed() => _machine.Configure(State.Reversed)
                .OnEntryFrom(_reversedTrgger, (Guid sessionId) =>
                {
                    var session = _actorService.GetSession(sessionId);
                    using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
                    {
                        var senderExists = db.Query<Model.Coin>().Where(s => s.TransactionId.Equals(session.SessionId)).Exists();
                        if (senderExists.Equals(true))
                        {
                            var sender = db.Query<Model.Coin>().Where(s => s.TransactionId.Equals(session.SessionId)).FirstOrDefault();
                            var success = db.Delete<Model.Coin>(sender.TransactionId);
                            if (success.Equals(false))
                            {
                                var message = $"Please check the logs for any details. Could not delete sender transaction {sender.PreImage}";

                                _logger.LogError(message);
                                throw new Exception(message);
                            }
                        }
                    }

                    _machine.Fire(Trigger.Failed);
                })
                .PermitReentry(Trigger.RollBack)
                .Permit(Trigger.Verify, State.Audited)
                .Permit(Trigger.Failed, State.Failure);

    }
}
