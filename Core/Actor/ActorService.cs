// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Tangram.Core.Wallet;
using Tangram.Core.Coin;
using Tangram.Core.Helper.Http;
using Dawn;
using Tangram.Core.Helper;
using Tangram.Core.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Polly;

namespace Tangram.Core.Actor
{
    public class ActorService : IActorService
    {
        private readonly ILogger _logger;
        private readonly IWalletService _walletService;
        private readonly IBuilderService _builderService;
        private readonly IConfigurationSection _apiNetworkSection;
        private readonly string _environment;
        private readonly Client _client;

        private ConcurrentDictionary<Guid, Session> Sessions { get; }

        public event MessagePumpEventHandler MessagePump;
        protected void OnMessagePump(MessagePumpEventArgs e)
        {
            if (MessagePump != null)
            {
                try
                {
                    MessagePump.Invoke(this, e);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                }
            }
        }

        public ActorService(IWalletService walletService, IBuilderService builderService, IConfiguration configuration, ILogger<ActorService> logger)
        {
            _walletService = walletService;
            _builderService = builderService;
            _logger = logger;
            _apiNetworkSection = configuration.GetSection(Constant.ApiNetwork);
            _environment = _apiNetworkSection.GetValue<string>(Constant.Environment);
            _client = GetClient(configuration);

            Sessions = new ConcurrentDictionary<Guid, Session>();
        }

        /// <summary>
        /// Get the client.
        /// </summary>
        public Client GetClient(IConfiguration configuration) => new Client(configuration, _logger);

        /// <summary>
        /// Get the session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Session GetSession(Guid sessionId) => Sessions.GetValueOrDefault(sessionId);

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        public async Task ReceivePayment(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            session = SessionAddOrUpdate(session);
            session.LastError = null;

            _ = Unlock(session.SessionId);
            _ = await Util.TriesUntilCompleted(async () => { return await ReceivePayment(session.SessionId, session.SenderAddress); }, 10, 100, true);
        }

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<bool> ReceivePayment(Guid sessionId, string address)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            var session = GetSession(sessionId);

            return true;
        }

        /// <summary>
        /// Adds or updates the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Session SessionAddOrUpdate(Session session)
        {
            var mSession = Sessions.AddOrUpdate(session.SessionId, session,
                            (Key, existingVal) =>
                            {
                                if (session != existingVal)
                                    throw new ArgumentException("Duplicate session ids are not allowed: {0}.", session.SessionId.ToString());

                                existingVal.Amount = session.Amount;
                                existingVal.Memo = session.Memo;
                                existingVal.RecipientAddress = session.RecipientAddress;
                                existingVal.SenderAddress = session.SenderAddress;
                                existingVal.HasFunds = session.HasFunds;

                                return existingVal;
                            });

            return mSession;
        }

        /// <summary>
        /// Sufficient funds.
        /// </summary>
        /// <returns>The Sufficient funds.</returns>
        public TaskResult<Session> SufficientFunds(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).HasValue();
            UpdateMessagePump("Checking available balance ...");

            var session = GetSession(sessionId);
            var balance = _walletService.AvailableBalance(session.Identifier, session.Passphrase);

            if (!balance.Success)
            {
                return TaskResult<Session>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Please check error logs for any details."
                }));
            }

            if (balance.Result < session.Amount)
            {
                return TaskResult<Session>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = new
                    {
                        available = balance,
                        spend = session.Amount
                    }
                }));
            }

            session.HasFunds = true;
            session = SessionAddOrUpdate(session);

            return TaskResult<Session>.CreateSuccess(session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<bool> Payment(Guid sessionId, string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();

            try
            {
                var redemptionKey = JsonConvert.DeserializeObject<RedemptionKey>(message);
                var block = await _client.GetAsync<BlockID>(redemptionKey.ScanAddress, RestApiMethod.Coin, new string[] { redemptionKey.PreImage });

                if (block.Result == null)
                    return false;

                var session = GetSession(sessionId);

                //var added = AddWalletTransaction(session.SessionId, block.Result.SignedBlock.Coin, redemptionKey.Amount, redemptionKey.Memo, redemptionKey.Blind.FromHexString(), TransactionType.Receive);
                //if (added.Equals(false))
                //    return false;

                redemptionKey.Blind.ZeroString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }

            return true;
        }

        /// <summary>
        /// Unlocks wallet.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<bool> Unlock(Guid sessionId)
        {
            UpdateMessagePump("Unlocking ...");

            try
            {
                SetRandomAddress(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// Sets random address.
        /// </summary>
        /// <returns>The random address.</returns>
        private void SetRandomAddress(Guid sessionId)
        {
            var session = GetSession(sessionId);
            using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());
            var keySets = db.Query<KeySet>().ToList();
            var results = keySets.Count();
            var rnd = new Random();
            var stealthAddress = keySets[rnd.Next(keySets.Count())].StealthAddress;

            session.SenderAddress = stealthAddress;
            SessionAddOrUpdate(session);

            for (int i = 0, keySetsCount = keySets.Count(); i < keySetsCount; i++)
            {
                KeySet key = keySets[i];
                key.ChainCode.ZeroString();
                key.RootKey.ZeroString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<bool> Spend(Guid sessionId)
        {
            var session = GetSession(sessionId);
            session.LastError = null;

            UpdateMessagePump("Busy building your transaction ...");

            var transaction = _walletService.SortChange(session);
            if (transaction.Success.Equals(false))
            {
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Not enough coin on a single chain for the request!"
                }));
            }

            var builder = _builderService.Build(session, transaction.Result);

            if (builder.Success.Equals(false))
            {
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = builder.Exception.Message
                }));
            }

            SessionAddOrUpdate(session);

            try
            {
                builder.Result.TransactionId = session.SessionId;

                using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
                {
                    var senderExists = db.Query<Model.Coin>().Where(s => s.TransactionId.Equals(session.SessionId)).Exists();
                    if (senderExists.Equals(false))
                    {
                        db.Insert(builder.Result);
                    }

                    var txExists = db.Query<Transaction>().Where(s => s.TransactionId.Equals(session.SessionId)).Exists();
                    if (txExists.Equals(false))
                    {
                        db.Insert(transaction.Result);
                    }
                }

                var addTxn = SaveTransaction(session.SessionId, transaction.Result);

                if (addTxn.Equals(false))
                {
                    throw new Exception("Failed to add transaction to wallet!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// Updates the message pump.
        /// </summary>
        /// <param name="message">Message.</param>
        public void UpdateMessagePump(string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();
            OnMessagePump(new MessagePumpEventArgs { Message = message });
            Task.Delay(100);
        }

        /// <summary>
        /// Post payload. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="payload"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        public async Task<TaskResult<byte[]>> PostArticle<T>(T payload, RestApiMethod api) where T : class
        {
            var response = await Policy
                .HandleResult<TaskResult<byte[]>>(message => !message.Success)
                .WaitAndRetryAsync(10, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Request failed. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                })
                .ExecuteAsync(() => _client.PostAsync(payload, api));

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public bool SaveTransaction(Guid sessionId, Transaction transaction)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(transaction, nameof(transaction)).NotNull();

            var session = GetSession(sessionId);
            using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
            {
                db.Insert(transaction);
            }

            return true;
        }


    }
}