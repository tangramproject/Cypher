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
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleBase;
using Sodium;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helper;
using TangramCypher.Helper.Http;
using TangramCypher.Helper.LibSodium;
using TangramCypher.ApplicationLayer.Coin;
using Dawn;
using TangramCypher.ApplicationLayer.Onion;
using TangramCypher.Model;
using Stateless;
using System.Collections.Concurrent;

namespace TangramCypher.ApplicationLayer.Actor
{
    public partial class ActorService : IActorService
    {
        private readonly IConfigurationSection apiRestSection;
        private readonly ILogger logger;
        private readonly IOnionServiceClient onionService;
        private readonly IWalletService walletService;
        private readonly ICoinService coinService;
        private readonly IUnitOfWork unitOfWork;
        private readonly Client client;
        private StateMachine<State, Trigger> machine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> verifyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> unlockTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> burnTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> commitReceiverTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> redemptionKeyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> publicKeyAgreementTrgger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> paymentTrgger;

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
                    logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                }
            }
        }

        public ActorService(IOnionServiceClient onionService, IWalletService walletService, ICoinService coinService, IConfiguration configuration, ILogger logger, IUnitOfWork unitOfWork)
        {
            this.onionService = onionService;
            this.walletService = walletService;
            this.coinService = coinService;
            this.logger = logger;
            this.unitOfWork = unitOfWork;

            client = onionService.OnionEnabled.Equals(1) ?
                new Client(logger, new DotNetTor.SocksPort.SocksPortHandler(onionService.SocksHost, onionService.SocksPort)) :
                new Client(logger);

            apiRestSection = configuration.GetSection(Constant.ApiGateway);

            Sessions = new ConcurrentDictionary<Guid, Session>();
            machine = new StateMachine<State, Trigger>(State.New);

            verifyTrigger = machine.SetTriggerParameters<Guid>(Trigger.Verify);
            unlockTrigger = machine.SetTriggerParameters<Guid>(Trigger.Unlock);
            burnTrigger = machine.SetTriggerParameters<Guid>(Trigger.Torch);
            commitReceiverTrigger = machine.SetTriggerParameters<Guid>(Trigger.Commit);
            redemptionKeyTrigger = machine.SetTriggerParameters<Guid>(Trigger.PrepareRedemptionKey);
            publicKeyAgreementTrgger = machine.SetTriggerParameters<Guid>(Trigger.PublicKeyAgreement);
            paymentTrgger = machine.SetTriggerParameters<Guid>(Trigger.PaymentAgreement);

            Configure();

            //Test().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Add async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="payload">Payload.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<TaskResult<T>> AddAsync<T>(T payload, RestApiMethod apiMethod)
        {
            Guard.Argument(payload, nameof(payload)).Equals(null);

            JObject jObject = null;
            var cts = new CancellationTokenSource();

            try
            {
                var baseAddress = GetBaseAddress();
                var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString());

                cts.CancelAfter(60000);
                jObject = await client.PostAsync(payload, baseAddress, path, cts.Token);

                if (jObject == null)
                {
                    TaskResult<T>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Please check the logs for any details."
                    }));
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
                return TaskResult<T>.CreateFailure(ex);
            }

            return TaskResult<T>.CreateSuccess(jObject.ToObject<T>());
        }

        /// <summary>
        /// Get async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="address">Address.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<T> GetAsync<T>(string address, RestApiMethod apiMethod)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            JObject jObject = null;
            var cts = new CancellationTokenSource();

            try
            {
                var baseAddress = GetBaseAddress();
                var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address);

                cts.CancelAfter(60000);
                jObject = await client.GetAsync<T>(baseAddress, path, cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
            }

            return jObject == null ? (default) : jObject.ToObject<T>();
        }

        /// <summary>
        /// Get range async.
        /// </summary>
        /// <returns>The range async.</returns>
        /// <param name="address">Address.</param>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<IEnumerable<T>> GetRangeAsync<T>(string address, int skip, int take, RestApiMethod apiMethod)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            IEnumerable<T> messages = null; ;
            var cts = new CancellationTokenSource();

            try
            {
                var baseAddress = GetBaseAddress();
                var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address, skip, take);

                cts.CancelAfter(60000);

                var returnMessages = await client.GetRangeAsync(baseAddress, path, cts.Token);

                messages = returnMessages?.Select(m => m.ToObject<T>());
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
            }

            return Task.FromResult(messages).Result;
        }

        public Session GetSession(Guid sessionId) => Sessions.GetValueOrDefault(sessionId);

        /// <summary>
        /// Decodes the address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="key">Key.</param>
        private Span<byte> DecodeAddress(string key) => Base58.Bitcoin.Decode(key);

        /// <summary>
        /// Sets the cypher.
        /// </summary>
        /// <returns>The cypher.</returns>
        /// <param name="message">Message.</param>
        /// <param name="pk">Pk.</param>
        private byte[] Cypher(string message, byte[] pk)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            return Cryptography.BoxSeal(Utilities.BinaryToHex(Encoding.UTF8.GetBytes(message)), pk);
        }

        /// <summary>
        /// Gets the shared key.
        /// </summary>
        /// <returns>The shared key.</returns>
        /// <param name="pk">Pk.</param>
        private byte[] ToSharedKey(SecureString secret, byte[] pk)
        {
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            using (var insecure = secret.Insecure())
            {
                return Cryptography.ScalarMult(Utilities.HexToBinary(insecure.Value), pk);
            }
        }

        /// <summary>
        /// Opens the sealed box.
        /// </summary>
        /// <returns>The box seal.</returns>
        /// <param name="cypher">Cypher.</param>
        /// <param name="pkSkDto">Pk sk dto.</param>
        private string OpenBoxSeal(string cypher, KeySetDto keySet)
        {
            Guard.Argument(cypher, nameof(cypher)).NotNull().NotEmpty();
            Guard.Argument(keySet, nameof(keySet)).NotNull();

            var pk = Encoding.UTF8.GetBytes(keySet.PublicKey);
            var sk = Encoding.UTF8.GetBytes(keySet.SecretKey);
            var message = Cryptography.OpenBoxSeal(Encoding.UTF8.GetBytes(cypher), new KeyPair(pk, sk));

            return message;
        }

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        public async Task ReceivePayment(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            session = SessionAddOrUpdate(session);
            session.LastError = null;

            await Unlock(session.SessionId);
            await Util.TriesUntilCompleted<bool>(async () => { return await ReceivePayment(session.SessionId, session.SenderAddress); }, 10, 100, true);
        }

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        /// <param name="address">Address.</param>
        private async Task<bool> ReceivePayment(Guid sessionId, string address, bool sharedKey = false, byte[] receiverPk = null)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            IEnumerable<MessageDto> messages;
            var msgAddress = string.Empty;
            var session = GetSession(sessionId);
            var pk = Util.FormatNetworkAddress(DecodeAddress(address).ToArray());

            msgAddress = sharedKey ? pk.ToHex() : Cryptography.GenericHashWithKey(pk.ToHex(), pk).ToHex();

            var track = await unitOfWork.GetTrackRepository().Get(session, StoreKey.PublicKey, pk.ToHex());

            UpdateMessagePump("Downloading messages ...");

            JObject count = null;
            int countValue = 0;
            if (track.Result == null)
                count = await GetAsync<JObject>(msgAddress, RestApiMethod.MessageCount);

            countValue = count == null ? 1 : count.Value<int>("count");

            messages = track == null
                ? await GetRangeAsync<MessageDto>(msgAddress, 0, countValue, RestApiMethod.MessageRange)
                : await GetRangeAsync<MessageDto>(msgAddress, track.Result.Skip, countValue, RestApiMethod.MessageRange);

            if (sharedKey)
                pk = Util.FormatNetworkAddress(receiverPk);

            switch (messages)
            {
                case null:
                    return false;
                default:
                    await CheckMessages(session.SessionId, address, messages, pk);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Receives payment from redemption key.
        /// </summary>
        /// <returns>The payment redemption key.</returns>
        /// <param name="cypher">Cypher.</param>
        public async Task<string> ReceivePaymentRedemptionKey(Session session, string cypher)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(cypher, nameof(cypher)).NotNull().NotEmpty();

            session = SessionAddOrUpdate(session);
            session.LastError = null;

            bool TestFromAddress() => Util.FormatNetworkAddress(DecodeAddress(session.SenderAddress).ToArray()) != null;
            if (TestFromAddress().Equals(false))
            {
                session.LastError = JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to read the recipient public key!"
                });
                return null;
            }

            await SetSecretKey(session.SessionId);

            var pk = Util.FormatNetworkAddress(DecodeAddress(session.SenderAddress).ToArray());
            var message = JObject.Parse(cypher).ToObject<MessageDto>();
            var rmsg = ReadMessage(session.SecretKey, message.Body, pk);
            var (isPayment, store) = ParseMessage(rmsg);
            var previousBal = await walletService.AvailableBalance(session.Identifier, session.MasterKey);
            var payment = await Payment(session.SessionId, store);

            if (payment)
            {
                var availableBal = await walletService.AvailableBalance(session.Identifier, session.MasterKey);
                var transaction = await walletService.LastTransaction(session.Identifier, session.MasterKey, TransactionType.Receive);
                return JsonConvert.SerializeObject(JObject.FromObject(new
                {
                    success = true,
                    message = new
                    {
                        previous = previousBal,
                        received = transaction.Amount,
                        available = availableBal
                    }
                }));
            }

            session.LastError = JObject.FromObject(new
            {
                success = false,
                message = new
                {
                    available = previousBal
                }
            });

            return null;
        }

        /// <summary>
        /// Checks the messages
        /// </summary>
        /// <param name="address"></param>
        /// <param name="messages"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private async Task CheckMessages(Guid sessionId, string address, IEnumerable<MessageDto> messages, byte[] pk)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();
            Guard.Argument(messages, nameof(messages)).NotNull("Failed to get messages.");
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            int skip = 1;
            var take = messages.Count();
            var session = GetSession(sessionId);

            foreach (var message in messages)
            {
                var msg = ReadMessage(session.SecretKey, message.Body, pk);
                var (isPayment, store) = ParseMessage(msg);

                if (!isPayment)
                {
                    var sharedKey = ToSharedKey(session.SecretKey, DecodeAddress(store).ToArray());
                    var msgAddress = EncodeAddress(Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk).ToHex());
                    var decode = DecodeAddress(address).ToArray();

                    Array.Clear(sharedKey, 0, sharedKey.Length);
                    store.ZeroString();

                    await ReceivePayment(session.SessionId, msgAddress, true, decode);
                    break;
                }

                UpdateMessagePump($"Processing payment {skip} of {take} ...");

                var payment = await Payment(session.SessionId, store);

                if (payment)
                {
                    var track = new TrackDto
                    {
                        PublicKey = pk.ToHex(),
                        Skip = skip,
                        Take = take
                    };

                    //TODO: Could possibility fail.. need recovery..
                    var added = await unitOfWork.GetTrackRepository().AddOrReplace(session, StoreKey.PublicKey, track.PublicKey, track);
                }

                skip++;
            }
        }

        private async Task<bool> Payment(Guid sessionId, string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();

            try
            {
                var redemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(message);
                var coin = await GetAsync<CoinDto>(redemptionKey.Hash, RestApiMethod.Coin);

                if (coin == null)
                    return false;

                var session = GetSession(sessionId);
                var (swap1, swap2) = coinService.CoinSwap(session.SecretKey, coin, redemptionKey);

                var keeperPass = await CoinPass(session.SecretKey, swap1, 3);
                if (keeperPass.Equals(false))
                    return false;

                //TODO: Above coin swap passes which writes to the ledger.. full pass could fail.. need recovery..
                var fullPass = await CoinPass(session.SecretKey, swap2, 1);
                if (fullPass.Equals(false))
                    return false;

                //TODO: Could possibility fail.. need recovery..
                var added = await AddWalletTransaction(session.SessionId, coin, redemptionKey.Amount, redemptionKey.Memo, redemptionKey.Blind.FromHex(), TransactionType.Receive);
                if (added.Equals(false))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }
        }

        /// <summary>
        ///  Checks if the coin equals the mode.
        /// </summary>
        /// <returns>The pass.</returns>
        /// <param name="swap">Swap.</param>
        /// <param name="mode">Mode.</param>
        private async Task<bool> CoinPass(SecureString secret, CoinDto swap, int mode)
        {
            Guard.Argument(swap, nameof(swap)).NotNull();
            Guard.Argument(mode, nameof(mode)).NotNegative();

            var canPass = false;
            var coin = coinService.DeriveCoin(swap, secret);
            var status = coinService.VerifyCoin(swap, coin);

            coin.Hash = coinService.Hash(coin).ToHex();
            coin.Network = walletService.NetworkAddress(coin).ToHex();

            if (status.Equals(mode))
            {
                var returnCoin = await AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin);
                if (returnCoin != null)
                    canPass = true;
            }

            return canPass;
        }

        /// <summary>
        /// Parses the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message">Message.</param>
        private (bool, string) ParseMessage(string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();

            try
            {
                var jObject = JObject.Parse(message);
                return (jObject.Value<bool>("payment"), jObject.Value<string>("store"));
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }
        }

        /// <summary>
        /// Establishes first time public key message.
        /// </summary>
        /// <returns>The pub key message.</returns>
        private async Task<TaskResult<bool>> PublicKeyAgreementMessage(Guid sessionId)
        {
            var session = GetSession(sessionId);
            session.LastError = null;

            UpdateMessagePump("Busy committing public key agreement ...");

            try
            {
                var pk = Util.FormatNetworkAddress(DecodeAddress(session.RecipientAddress).ToArray());
                var msgAddress = Cryptography.GenericHashWithKey(pk.ToHex(), pk);
                var senderPk = session.PublicKey.ToUnSecureString();
                var innerMessage = JObject.FromObject(new
                {
                    payment = false,
                    store = EncodeAddress(senderPk)
                });
                var paddedBuf = Cryptography.Pad(innerMessage.ToString());
                var cypher = Cypher(Encoding.UTF8.GetString(paddedBuf), pk);
                var payload = new MessageDto
                {
                    Address = msgAddress.ToBase64(),
                    Body = cypher.ToBase64(),
                    TransactionId = session.SessionId
                };

                //TODO:.. Need steps.. If Success
                var addPubKeyAgreement = await unitOfWork
                                    .GetPublicKeyAgreementRepository()
                                    .Put(session, StoreKey.TransactionIdKey, payload.TransactionId.ToString(), payload);
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// Encodes the address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="pk">Pk.</param>
        private string EncodeAddress(string pk)
        {
            Guard.Argument(pk, nameof(pk)).NotNull().NotEmpty();

            string address = null;

            try
            {
                address = Base58.Bitcoin.Encode(Utilities.HexToBinary(pk));
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }

            return address;
        }

        private async Task<TaskResult<bool>> Unlock(Guid sessionId)
        {
            UpdateMessagePump("Unlocking ...");

            try
            {
                await SetRandomAddress(sessionId);
                await SetSecretKey(sessionId);
                await SetPublicKey(sessionId);
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        //TODO: Cleanup
        private async Task<TaskResult<bool>> CommitReceiver(Guid sessionId)
        {
            var session = GetSession(sessionId);
            session.LastError = null;

            UpdateMessagePump("Busy committing receiver coin ...");

            CoinDto receiverCoin = null;
            byte[] blind = null;

            var taskResult = coinService.Receiver(session.MasterKey, session.Amount, out receiverCoin, out blind);
            if (!taskResult.Success)
            {
                return TaskResult<bool>.CreateFailure(taskResult.Exception);
            }

            try
            {
                receiverCoin.Network = walletService.NetworkAddress(receiverCoin).ToHex();
                receiverCoin.TransactionId = session.SessionId;

                var receiverRepo = unitOfWork.GetReceiverRepository();
                var purchaseRepo = unitOfWork.GetPurchaseRepository();
                //TODO: Need steps.. If Success
                var addReceiver = await receiverRepo.Put(session, StoreKey.TransactionIdKey, receiverCoin.TransactionId.ToString(), receiverCoin);
                var purchase = await purchaseRepo.Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

                purchase.Result.Blind = blind.ToHex();
                var addPurchase = await purchaseRepo.AddOrReplace(session, StoreKey.TransactionIdKey, session.SessionId.ToString(), purchase.Result);

                purchase.Result.Blind.ZeroString();
                Array.Clear(blind, 0, blind.Length);
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
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
        private async Task SetRandomAddress(Guid sessionId)
        {
            var session = GetSession(sessionId);
            var rnd = await unitOfWork.GetKeySetRepository().RandomAddress(session.Identifier, session.MasterKey);

            session.SenderAddress = rnd;
            SessionAddOrUpdate(session);
        }

        //TODO: Need a better way of handling secret key.. 
        /// <summary>
        /// Sets the secret key.
        /// </summary>
        /// <returns>The secret key.</returns>
        private async Task SetSecretKey(Guid sessionId)
        {
            var session = GetSession(sessionId);
            var keySet = await unitOfWork.GetKeySetRepository().Get(session, StoreKey.AddressKey, session.SenderAddress);

            session.SecretKey = keySet.Result.SecretKey.ToSecureString();
            SessionAddOrUpdate(session);
        }

        /// <summary>
        /// Sets the public key.
        /// </summary>
        /// <returns>The public key.</returns>
        private async Task SetPublicKey(Guid sessionId)
        {
            var session = GetSession(sessionId);
            var keySet = await unitOfWork.GetKeySetRepository().Get(session, StoreKey.AddressKey, session.SenderAddress);

            session.PublicKey = keySet.Result.PublicKey.ToSecureString();
            SessionAddOrUpdate(session);
        }

        /// <summary>
        /// Sufficient funds.
        /// </summary>
        /// <returns>The Sufficient funds.</returns>
        private async Task<TaskResult<Session>> SufficientFunds(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).HasValue();
            UpdateMessagePump("Checking funds ...");

            var session = GetSession(sessionId);
            var balance = await walletService.AvailableBalance(session.Identifier, session.MasterKey);

            if (balance.Success)
                if (balance.Result >= session.Amount)
                {
                    session.SufficientFunds = true;
                    session = SessionAddOrUpdate(session);
                }
                else
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
            else
            {
                return TaskResult<Session>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Please check the error logs for any details."
                }));
            }

            return TaskResult<Session>.CreateSuccess(session);
        }

        /// <summary>
        /// Builds the redemption key message.
        /// </summary>
        /// <returns>The redemption key message.</returns>
        private async Task<TaskResult<bool>> RedemptionKeyMessage(Guid sessionId)
        {
            var session = GetSession(sessionId);
            session.LastError = null;

            UpdateMessagePump("Preparing redemption key ...");

            try
            {
                //TODO:.. Need steps.. If Success
                var purchase = await unitOfWork
                                .GetPurchaseRepository()
                                .Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());
                //TODO:.. Need steps.. If Success
                var receiverCoin = await unitOfWork
                                    .GetReceiverRepository()
                                    .Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

                var (key1, key2) = coinService.HotRelease(receiverCoin.Result.Version, receiverCoin.Result.Stamp, session.MasterKey);
                var redemption = new RedemptionKeyDto
                {
                    Amount = session.Amount,
                    Blind = purchase.Result.Blind,
                    Hash = receiverCoin.Result.Hash,
                    Key1 = key1,
                    Key2 = key2,
                    Memo = session.Memo,
                    Stamp = receiverCoin.Result.Stamp
                };
                var innerMessage = JObject.FromObject(new
                {
                    payment = true,
                    store = JsonConvert.SerializeObject(redemption)
                });
                var paddedBuf = Cryptography.Pad(innerMessage.ToString());
                var pk = Util.FormatNetworkAddress(DecodeAddress(session.RecipientAddress).ToArray());
                var cypher = Cypher(Encoding.UTF8.GetString(paddedBuf), pk);
                var sharedKey = ToSharedKey(session.SecretKey, pk.ToArray());
                var msgAddress = Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk);
                var messageStore = new MessageStoreDto
                {
                    DateTime = DateTime.Now,
                    Hash = redemption.Hash,
                    Memo = session.Memo,
                    Message = new MessageDto
                    {
                        Address = msgAddress.ToBase64(),
                        Body = cypher.ToBase64()
                    },
                    PublicKey = Sodium.Utilities.BinaryToHex(DecodeAddress(session.RecipientAddress).ToArray()),
                    TransactionId = session.SessionId
                };

                //TODO:.. Need steps.. If Success
                var addRedemption = await unitOfWork
                                    .GetRedemptionRepository()
                                    .Put(session, StoreKey.TransactionIdKey, session.SessionId.ToString(), messageStore);

                key1.ZeroString();
                key2.ZeroString();
                redemption.Key1.ZeroString();
                redemption.Key2.ZeroString();

                Array.Clear(paddedBuf, 0, paddedBuf.Length);
                Array.Clear(sharedKey, 0, sharedKey.Length);

                innerMessage = null;
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        //TODO: Needs refactoring...
        /// <summary>
        /// Spend.
        /// </summary>
        /// <returns>The spend.</returns>
        private async Task<TaskResult<bool>> Burn(Guid sessionId)
        {
            var session = GetSession(sessionId);
            session.LastError = null;

            UpdateMessagePump("Busy committing sender coin ...");

            bool TestToAddress() => Util.FormatNetworkAddress(DecodeAddress(session.RecipientAddress).ToArray()) != null;
            if (TestToAddress().Equals(false))
            {
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to read the recipient public key!"
                }));
            }

            var purchase = await walletService.SortChange(session);
            if (purchase.Success.Equals(false))
            {
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Not enough coin on a sigle chain for the request!"
                }));
            }

            var sender = await coinService.Sender(session, purchase.Result);
            if (sender.Success.Equals(false))
            {
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to build sender coin!"
                }));
            }

            //TODO: Cleanup..
            sender.Result.Network = walletService.NetworkAddress(sender.Result).ToHex();
            sender.Result.TransactionId = session.SessionId;

            //TODO: Need steps.. If Success
            var addSender = await unitOfWork
                               .GetSenderRepository()
                               .Put(session, StoreKey.TransactionIdKey, sender.Result.TransactionId.ToString(), sender.Result);
            //TODO: Need steps.. If Success
            var addPurchase = await unitOfWork
                               .GetPurchaseRepository()
                               .Put(session, StoreKey.TransactionIdKey, purchase.Result.TransactionId.ToString(), purchase.Result);
            //TODO: Need steps.. If Success
            var addTxn = await AddWalletTransaction(session.SessionId, sender.Result, purchase.Result.Input, session.Memo, null, TransactionType.Send);

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// Add the Wallet transaction.
        /// </summary>
        /// <returns>The Wallet transaction.</returns>
        /// <param name="coin">Coin.</param>
        /// <param name="transactionType">Transaction type.</param>
        private async Task<bool> AddWalletTransaction(Guid sessionId, CoinDto coin, ulong total, string memoText, byte[] blind, TransactionType transactionType)
        {
            Guard.Argument(coin, nameof(coin)).NotNull();
            Guard.Argument(total, nameof(total)).NotNegative();

            CoinDto formattedCoin = null;

            try
            { formattedCoin = coin.FormatCoinFromBase64(); }
            catch (FormatException)
            { formattedCoin = coin; }

            var txn = new TransactionDto
            {
                TransactionId = Guid.NewGuid().ToString(),
                Amount = total,
                Blind = blind == null ? string.Empty : blind.ToHex(),
                Commitment = formattedCoin.Envelope.Commitment,
                Hash = formattedCoin.Hash,
                Stamp = formattedCoin.Stamp,
                Version = formattedCoin.Version,
                TransactionType = transactionType,
                Memo = memoText,
                DateTime = DateTime.Now
            };

            var session = GetSession(sessionId);
            var addTxn = await unitOfWork.GetTransactionRepository().Put(session, StoreKey.HashKey, txn.Hash, txn);

            return addTxn.Success;
        }

        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="body">Body.</param>
        /// <param name="pk">Pk.</param>
        private string ReadMessage(SecureString secret, string body, byte[] pk)
        {
            Guard.Argument(body, nameof(body)).NotNull().NotEmpty();
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            string unpadded = null;

            using (var insecureSk = secret.Insecure())
            {
                try
                {
                    var message = Utilities.HexToBinary(Encoding.UTF8.GetString(Convert.FromBase64String(body)));
                    var opened = Cryptography.OpenBoxSeal(message, new KeyPair(pk, Utilities.HexToBinary(insecureSk.Value)));

                    unpadded = Encoding.UTF8.GetString((Cryptography.Unpad(opened.FromHex())));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                    throw ex;
                }
            }

            return unpadded;
        }

        /// <summary>
        /// Get the base address.
        /// </summary>
        /// <returns>The base address.</returns>
        private Uri GetBaseAddress()
        {
            return new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
        }

        /// <summary>
        /// Updates the message pump.
        /// </summary>
        /// <param name="message">Message.</param>
        private void UpdateMessagePump(string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();
            OnMessagePump(new MessagePumpEventArgs { Message = message });
            Task.Delay(500);
        }

        private Session SessionAddOrUpdate(Session session)
        {
            var mSession = Sessions.AddOrUpdate(session.SessionId, session,
                            (Key, existingVal) =>
                            {
                                if (session != existingVal)
                                    throw new ArgumentException("Duplicate session ids are not allowed: {0}.", session.SessionId.ToString());

                                existingVal.Amount = session.Amount;
                                existingVal.ForwardMessage = session.ForwardMessage;
                                existingVal.Memo = session.Memo;
                                existingVal.PublicKey = session.PublicKey;
                                existingVal.RecipientAddress = session.RecipientAddress;
                                existingVal.SecretKey = session.SecretKey;
                                existingVal.SenderAddress = session.SenderAddress;
                                existingVal.SufficientFunds = session.SufficientFunds;

                                return existingVal;
                            });

            return mSession;
        }

        private async Task<IEnumerable<T>> PostParallel<T>(IEnumerable<T> payload, RestApiMethod apiMethod)
        {
            var tasks = new List<Task<TaskResult<IEnumerable<T>>>>();
            var batchSize = 100;
            int numberOfBatches = (int)System.Math.Ceiling((double)payload.Count() / batchSize);

            for (int i = 0; i < numberOfBatches; i++)
            {
                var current = payload.Skip(i * batchSize).Take(batchSize);
                tasks.Add(AddAsync(payload, apiMethod));
            }

            return (await Task.WhenAll(tasks)).SelectMany(u => u.Result);
        }

        private async Task Test()
        {
            var session = new Session("id_ee75d7b59f76b55601d8e8611118aa0d".ToSecureString(), "its pilferer sung will vellum things concoct calmly the futile lover".ToSecureString())
            {
                Amount = 70000000000000
            };

            session = SessionAddOrUpdate(session);

            coinService.Receiver(session.MasterKey, session.Amount, out CoinDto coin, out byte[] blind);

            coin.Hash = coinService.Hash(coin).ToHex();
            coin.Network = walletService.NetworkAddress(coin).ToHex();

            var c = await Util.TriesUntilCompleted<TaskResult<CoinDto>>(async () =>
            {
                return await AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin);
            }, 10, 100);

            var added = await AddWalletTransaction(session.SessionId, coin, session.Amount, "Added Test Coin", blind, TransactionType.Receive);
        }

    }
}