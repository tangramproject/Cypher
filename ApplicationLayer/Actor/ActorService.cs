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
        private readonly ILogger logger;
        private readonly IOnionServiceClient onionService;
        private readonly IWalletService walletService;
        private readonly ICoinService coinService;
        private readonly IUnitOfWork unitOfWork;
        private StateMachine<State, Trigger> machine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> verifyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> unlockTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> burnTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> commitReceiverTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> redemptionKeyTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> publicKeyAgreementTrgger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> paymentTrgger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> reversedTrgger;

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

            Client = onionService.OnionEnabled.Equals(1) ?
                new Client(configuration, logger, new DotNetTor.SocksPort.SocksPortHandler(onionService.SocksHost, onionService.SocksPort)) :
                new Client(configuration, logger);

            Sessions = new ConcurrentDictionary<Guid, Session>();
            machine = new StateMachine<State, Trigger>(State.New);

            verifyTrigger = machine.SetTriggerParameters<Guid>(Trigger.Verify);
            unlockTrigger = machine.SetTriggerParameters<Guid>(Trigger.Unlock);
            burnTrigger = machine.SetTriggerParameters<Guid>(Trigger.Torch);
            commitReceiverTrigger = machine.SetTriggerParameters<Guid>(Trigger.Commit);
            redemptionKeyTrigger = machine.SetTriggerParameters<Guid>(Trigger.PrepareRedemptionKey);
            publicKeyAgreementTrgger = machine.SetTriggerParameters<Guid>(Trigger.PublicKeyAgreement);
            paymentTrgger = machine.SetTriggerParameters<Guid>(Trigger.PaymentAgreement);
            reversedTrgger = machine.SetTriggerParameters<Guid>(Trigger.RollBack);

            Configure();

            // Test().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get the client.
        /// </summary>
        public Client Client { get; }

        /// <summary>
        /// Get the session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
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
        /// Opens the box seal.
        /// </summary>
        /// <returns>The box seal.</returns>
        /// <param name="cypher">Cypher.</param>
        /// <param name="keySet">Key set.</param>
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

            _ = await Unlock(session.SessionId);
            _ = await Util.TriesUntilCompleted(async () => { return await ReceivePayment(session.SessionId, session.SenderAddress); }, 10, 100, true);
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
            var session = GetSession(sessionId);
            var pk = Util.FormatNetworkAddress(DecodeAddress(address).ToArray());

            string msgAddress = sharedKey ? pk.ToHex() : Cryptography.GenericHashWithKey(pk.ToHex(), pk).ToHex();

            //TODO.. implement better track system..
            //var track = await unitOfWork.GetTrackRepository().Get(session, StoreKey.PublicKey, pk.ToHex());

            UpdateMessagePump("Downloading messages ...");
            TaskResult<JObject> count = await Client.GetAsync<JObject>(msgAddress, RestApiMethod.MessageCount);
            int countValue = count.Success ? count.Result.Value<int>("count") : 1;

            // messages = track.Result == null
            //     ? await client.GetRangeAsync<MessageDto>(msgAddress, 0, countValue, RestApiMethod.MessageRange)
            //     : await client.GetRangeAsync<MessageDto>(msgAddress, track.Result.Skip, countValue, RestApiMethod.MessageRange);

            messages = await Client.GetRangeAsync<MessageDto>(msgAddress, 0, countValue, RestApiMethod.MessageRange);

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
        public async Task<TaskResult<bool>> ReceivePaymentRedemptionKey(Session session, string cypher)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(cypher, nameof(cypher)).NotNull().NotEmpty();

            session = SessionAddOrUpdate(session);
            session.LastError = null;

            try
            {
                bool TestFromAddress() => Util.FormatNetworkAddress(DecodeAddress(session.SenderAddress).ToArray()) != null;
                if (TestFromAddress().Equals(false))
                {
                    return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Failed to read the recipient public key!"
                    }));
                }

                await SetSecretKey(session.SessionId);

                var pk = Util.FormatNetworkAddress(DecodeAddress(session.SenderAddress).ToArray());
                var message = JObject.Parse(cypher).ToObject<MessageDto>();
                var rmsg = ReadMessage(session.SecretKey, message.Body, pk);

                ParseMessage(rmsg, out bool isPayment, out string store);

                var previousBal = await walletService.AvailableBalance(session.Identifier, session.MasterKey);
                var payment = await Payment(session.SessionId, store);

                if (payment)
                {
                    var availableBal = await walletService.AvailableBalance(session.Identifier, session.MasterKey);
                    var transaction = await walletService.LastTransaction(session.Identifier, session.MasterKey, TransactionType.Receive);

                    return TaskResult<bool>.CreateSuccess(JObject.FromObject(new
                    {
                        success = true,
                        message = new
                        {
                            previous = previousBal,
                            memo = transaction.Memo,
                            received = transaction.Amount,
                            available = availableBal
                        }
                    }));
                }

                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = new
                    {
                        available = previousBal
                    }
                }));
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(ex);
            }
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

                ParseMessage(msg, out bool isPayment, out string store);

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

                    var addTrack = await unitOfWork.GetTrackRepository().AddOrReplace(session, StoreKey.PublicKey, track.PublicKey, track);
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
                var coinResult = await Client.GetAsync<CoinDto>(redemptionKey.Hash, RestApiMethod.Coin);

                if (coinResult.Result == null)
                    return false;

                var session = GetSession(sessionId);
                var (swap1, swap2) = coinService.CoinSwap(session.SecretKey, coinResult.Result, redemptionKey);

                var keeperPass = await CoinPass(session.SecretKey, swap1, 3);
                if (keeperPass.Equals(false))
                    return false;

                //TODO: Above coin swap passes which writes to the ledger.. full pass could fail.. need recovery..
                var fullPass = await CoinPass(session.SecretKey, swap2, 1);
                if (fullPass.Equals(false))
                    return false;

                //TODO: Could possibility fail.. need recovery..
                var added = await AddWalletTransaction(session.SessionId, coinResult.Result, redemptionKey.Amount, redemptionKey.Memo, redemptionKey.Blind.FromHex(), TransactionType.Receive);
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

            //TODO.... remove
            coin.Network = walletService.NetworkAddress(coin).ToHex();
            coin.Envelope.RangeProof = walletService.NetworkAddress(coin).ToHex();

            if (status.Equals(mode))
            {
                var returnCoin = await Client.AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin);
                if (returnCoin.Result != null)
                    canPass = true;
            }

            return canPass;
        }

        /// <summary>
        /// Parses the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message">Message.</param>
        private void ParseMessage(string message, out bool isPayment, out string store)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();
            try
            {
                var jObject = JObject.Parse(message);
                isPayment = jObject.Value<bool>("payment");
                store = jObject.Value<string>("store");
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

                var addPubKeyAgreement = await unitOfWork
                                    .GetPublicKeyAgreementRepository()
                                    .Put(session, StoreKey.TransactionIdKey, payload.TransactionId.ToString(), payload);

                if (addPubKeyAgreement.Success.Equals(false))
                    throw addPubKeyAgreement.Exception;
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

            string address;

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

        private async Task<TaskResult<bool>> CommitReceiver(Guid sessionId)
        {
            var session = GetSession(sessionId);
            session.LastError = null;

            UpdateMessagePump("Busy committing receiver coin ...");

            try
            {
                var receiverResult = coinService.Receiver(session.MasterKey, session.Amount, out CoinDto receiverCoin, out byte[] blind);
                if (receiverResult.Success.Equals(false))
                {
                    throw receiverResult.Exception;
                }

                // TODO: Should add this from the source..
                receiverCoin.Network = walletService.NetworkAddress(receiverCoin).ToHex();
                receiverCoin.TransactionId = session.SessionId;

                var putReceiver = await unitOfWork
                                    .GetReceiverRepository()
                                    .Put(session, StoreKey.TransactionIdKey, receiverCoin.TransactionId.ToString(), receiverCoin);

                if (putReceiver.Success.Equals(false))
                {
                    throw putReceiver.Exception;
                }

                var purchaseRepo = unitOfWork.GetPurchaseRepository();
                var getPurchase = await purchaseRepo.Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

                if (getPurchase.Success)
                {
                    getPurchase.Result.Blind = blind.ToHex();

                    var addPurchase = await purchaseRepo.AddOrReplace(session, StoreKey.TransactionIdKey, session.SessionId.ToString(), getPurchase.Result);

                    if (addPurchase.Success.Equals(false))
                    {
                        throw addPurchase.Exception;
                    }

                    getPurchase.Result.Blind.ZeroString();
                    Array.Clear(blind, 0, blind.Length);
                }
                else
                {
                    throw getPurchase.Exception;
                }
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

                var getPurchase = await unitOfWork
                                .GetPurchaseRepository()
                                .Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

                if (getPurchase.Success.Equals(false))
                {
                    throw getPurchase.Exception;
                }

                var getReceiver = await unitOfWork
                                    .GetReceiverRepository()
                                    .Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

                if (getReceiver.Success.Equals(false))
                {
                    throw getReceiver.Exception;
                }

                var (key1, key2) = coinService.HotRelease(getReceiver.Result.Version, getReceiver.Result.Stamp, session.MasterKey);
                var redemption = new RedemptionKeyDto
                {
                    Amount = session.Amount,
                    Blind = getPurchase.Result.Blind,
                    Hash = getReceiver.Result.Hash,
                    Key1 = key1,
                    Key2 = key2,
                    Memo = session.Memo,
                    Stamp = getReceiver.Result.Stamp
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
                    PublicKey = Utilities.BinaryToHex(DecodeAddress(session.RecipientAddress).ToArray()),
                    TransactionId = session.SessionId
                };

                var putRedemption = await unitOfWork
                                    .GetRedemptionRepository()
                                    .Put(session, StoreKey.TransactionIdKey, session.SessionId.ToString(), messageStore);

                if (putRedemption.Success.Equals(false))
                {
                    throw putRedemption.Exception;
                }

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
                    message = sender.Exception.Message
                }));
            }

            try
            {
                //TODO: Cleanup..
                sender.Result.Network = walletService.NetworkAddress(sender.Result).ToHex();
                sender.Result.TransactionId = session.SessionId;

                var putSender = await unitOfWork
                                   .GetSenderRepository()
                                   .Put(session, StoreKey.TransactionIdKey, sender.Result.TransactionId.ToString(), sender.Result);

                if (putSender.Success.Equals(false))
                {
                    throw putSender.Exception;
                }

                var putPurchase = await unitOfWork
                                   .GetPurchaseRepository()
                                   .Put(session, StoreKey.TransactionIdKey, purchase.Result.TransactionId.ToString(), purchase.Result);

                if (putPurchase.Success.Equals(false))
                {
                    throw putPurchase.Exception;
                }

                var addTxn = await AddWalletTransaction(session.SessionId, sender.Result, purchase.Result.Input, session.Memo, null, TransactionType.Send);

                if (addTxn.Equals(false))
                {
                    throw new Exception("Failed to add transaction to wallet!");
                }
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
        /// Add the Wallet transaction.
        /// </summary>
        /// <returns>The Wallet transaction.</returns>
        /// <param name="coin">Coin.</param>
        /// <param name="transactionType">Transaction type.</param>
        private async Task<bool> AddWalletTransaction(Guid sessionId, CoinDto coin, ulong total, string memoText, byte[] blind, TransactionType transactionType)
        {
            Guard.Argument(coin, nameof(coin)).NotNull();
            Guard.Argument(total, nameof(total)).NotNegative();
            CoinDto formattedCoin;
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
        /// Updates the message pump.
        /// </summary>
        /// <param name="message">Message.</param>
        private void UpdateMessagePump(string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();
            OnMessagePump(new MessagePumpEventArgs { Message = message });
            Task.Delay(100);
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

        private async Task<IEnumerable<TaskResult<T>>> PostParallel<T>(IEnumerable<T> payload, RestApiMethod apiMethod)
        {
            var tasks = payload.Select(async p => await Util.TriesUntilCompleted(async () => { return await Client.AddAsync(p, apiMethod); }, 10, 100));
            return await Task.WhenAll(tasks);
        }

        private async Task<TaskResult<T>> PostArticle<T>(T payload, RestApiMethod api) where T : class
        {
            var result = await Util.TriesUntilCompleted(async () => { return await Client.AddAsync(payload, api); }, 10, 100);
            return result;
        }

        private async Task Test()         {             try             {                 var session = new Session("id_716f477592d591439c25a948033b0b8f".ToSecureString(), "the grim schism drawled that iffy one gibbets the positron".ToSecureString())                 {                     Amount = 20000000000000                 };                  session = SessionAddOrUpdate(session);                  coinService.Receiver(session.MasterKey, session.Amount, out CoinDto coin, out byte[] blind);                  coin.Hash = coinService.Hash(coin).ToHex();                 coin.Network = walletService.NetworkAddress(coin).ToHex();                 coin.Envelope.RangeProof = walletService.NetworkAddress(coin).ToHex();                  var coinResult = await PostArticle(coin.FormatCoinToBase64(), RestApiMethod.PostCoin);                  if (coinResult.Success.Equals(false))                 {                     throw new Exception(JsonConvert.SerializeObject(coinResult.NonSuccessMessage));                 }                  var added = await AddWalletTransaction(session.SessionId, coinResult.Result, session.Amount, "Check running total..", blind, TransactionType.Receive);                  if (added.Equals(false))                 {                     throw new Exception("Transaction wallet failed to add!");                 }              }             catch (Exception ex)             {                 throw ex;             }          }
    }
}