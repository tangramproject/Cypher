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

namespace TangramCypher.ApplicationLayer.Actor
{
    public class ActorService : IActorService
    {
        protected SecureString masterKey;
        protected string toAdress;
        protected string fromAddress;
        protected ulong amount;
        protected string memo;
        protected SecureString secretKey;
        protected SecureString publicKey;
        protected SecureString identifier;

        private JObject lastError;

        private readonly IConfigurationSection apiRestSection;
        private readonly ILogger logger;
        private readonly IOnionServiceClient onionService;
        private readonly IWalletService walletService;
        private readonly ICoinService coinService;
        private readonly Client client;

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

        public ActorService(IOnionServiceClient onionService, IWalletService walletService, ICoinService coinService, IConfiguration configuration, ILogger logger)
        {
            this.onionService = onionService;
            this.walletService = walletService;
            this.coinService = coinService;
            this.logger = logger;

            client = onionService.OnionEnabled.Equals(1) ?
                new Client(logger, new DotNetTor.SocksPort.SocksPortHandler(onionService.SocksHost, onionService.SocksPort)) :
                new Client(logger);

            apiRestSection = configuration.GetSection(Constant.ApiGateway);
        }

        /// <summary>
        /// Add async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="payload">Payload.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<T> AddAsync<T>(T payload, RestApiMethod apiMethod)
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
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
            }

            return jObject == null ? (default) : jObject.ToObject<T>();
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

        /// <summary>
        /// Gets the Amount instance.
        /// </summary>
        /// <returns>The amount.</returns>
        public ulong Amount() => amount;

        /// <summary>
        /// Sets the specified Amount value.
        /// </summary>
        /// <returns>The amount.</returns>
        /// <param name="value">Value.</param>
        public ActorService Amount(double value)
        {
            Guard.Argument(value, nameof(value)).NotZero().NotNegative();

            try
            {
                var parts = value.ToString().Split(new char[] { '.', ',' });
                var part1 = (ulong)Math.Truncate(value);

                if (parts.Length.Equals(1))
                    amount = walletService.MulWithNaT(part1);
                else
                {
                    var part2 = (ulong)((value - part1) * UInt64.Parse("1".PadRight(parts[1].Length + 1, '0')) + 0.5);
                    amount = walletService.MulWithNaT(part1) + UInt64.Parse(part2.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw ex;
            }

            return this;
        }

        /// <summary>
        /// Checks the balance.
        /// </summary>
        /// <returns>The balance.</returns>
        public async Task<ulong> CheckBalance() => await walletService.AvailableBalance(Identifier(), MasterKey());

        /// <summary>
        /// Decodes the address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="key">Key.</param>
        public Span<byte> DecodeAddress(string key) => Base58.Bitcoin.Decode(key);

        /// <summary>
        /// Gets the master key instance.
        /// </summary>
        /// <returns>The from.</returns>
        public SecureString MasterKey() => masterKey;

        /// <summary>
        /// Sets the specified password.
        /// </summary>
        /// <returns>The from.</returns>
        /// <param name="password">Password.</param>
        public ActorService MasterKey(SecureString password)
        {
            masterKey = Guard.Argument(password, nameof(password)).NotNull();
            return this;
        }

        /// <summary>
        /// Sets the cypher.
        /// </summary>
        /// <returns>The cypher.</returns>
        /// <param name="message">Message.</param>
        /// <param name="pk">Pk.</param>
        public byte[] Cypher(string message, byte[] pk)
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
        public byte[] ToSharedKey(byte[] pk)
        {
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            using (var insecure = SecretKey().Insecure())
            {
                return Cryptography.ScalarMult(Utilities.HexToBinary(insecure.Value), pk);
            }
        }

        /// <summary>
        /// Gets the wallet identifier instance.
        /// </summary>
        /// <returns>The identifier.</returns>
        public SecureString Identifier() => identifier;

        /// <summary>
        /// Sets the specified walletId.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="walletId">Wallet identifier.</param>
        public ActorService Identifier(SecureString walletId)
        {
            identifier = Guard.Argument(walletId, nameof(walletId)).NotNull();
            return this;
        }

        /// <summary>
        /// gets the Memo text instance.
        /// </summary>
        /// <returns>The memo.</returns>
        public string Memo() => memo;

        /// <summary>
        /// Sets the specified memo text.
        /// </summary>
        /// <returns>The memo.</returns>
        /// <param name="memo">Text.</param>
        public ActorService Memo(string memo)
        {
            if (memo == null)
                this.memo = string.Empty;

            this.memo = Guard.Argument(memo, nameof(memo)).MaxLength(64);
            return this;
        }

        /// <summary>
        /// Opens the sealed box.
        /// </summary>
        /// <returns>The box seal.</returns>
        /// <param name="cypher">Cypher.</param>
        /// <param name="pkSkDto">Pk sk dto.</param>
        public string OpenBoxSeal(string cypher, PkSkDto pkSkDto)
        {
            Guard.Argument(cypher, nameof(cypher)).NotNull().NotEmpty();
            Guard.Argument(pkSkDto, nameof(pkSkDto)).NotNull();

            var pk = Encoding.UTF8.GetBytes(pkSkDto.PublicKey);
            var sk = Encoding.UTF8.GetBytes(pkSkDto.SecretKey);
            var message = Cryptography.OpenBoxSeal(Encoding.UTF8.GetBytes(cypher), new KeyPair(pk, sk));

            return message;
        }

        /// <summary>
        /// Gets the poublic key.
        /// </summary>
        /// <returns>The key.</returns>
        public SecureString PublicKey() => publicKey;

        /// <summary>
        /// Sets the public key.
        /// </summary>
        /// <returns>The key.</returns>
        /// <param name="pk">Pk.</param>
        public ActorService PublicKey(SecureString pk)
        {
            publicKey = Guard.Argument(pk, nameof(pk)).NotNull("Public key cannot be null!");
            return this;
        }

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        public async Task ReceivePayment()
        {
            await SetSecretKey();
            await SetPublicKey();

            await Util.TriesUntilCompleted<bool>(async () => { return await ReceivePayment(fromAddress); }, 10, 100, true);
        }

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        /// <param name="address">Address.</param>
        private async Task<bool> ReceivePayment(string address, bool sharedKey = false, byte[] receiverPk = null)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            IEnumerable<NotificationDto> notifications;
            var notificationAddress = string.Empty;
            var pk = Util.FormatNetworkAddress(DecodeAddress(address).ToArray());

            notificationAddress = sharedKey ? pk.ToHex() : Cryptography.GenericHashWithKey(pk.ToHex(), pk).ToHex();

            var messageTrack = await walletService.MessageTrack(Identifier(), MasterKey(), pk.ToHex());

            UpdateMessagePump("Downloading messages ...");

            JObject count = null;
            int countValue = 0;
            if (messageTrack == null)
                count = await GetAsync<JObject>(notificationAddress, RestApiMethod.MessageCount);

            countValue = count == null ? 1 : count.Value<int>("count");

            notifications = messageTrack == null
                ? await GetRangeAsync<NotificationDto>(notificationAddress, 0, countValue, RestApiMethod.MessageRange)
                : await GetRangeAsync<NotificationDto>(notificationAddress, messageTrack.Skip, countValue, RestApiMethod.MessageRange);

            if (sharedKey)
                pk = Util.FormatNetworkAddress(receiverPk);

            switch (notifications)
            {
                case null:
                    return false;
                default:
                    await CheckNotifications(address, notifications, pk);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Receives payment from redemption key.
        /// </summary>
        /// <returns>The payment redemption key.</returns>
        /// <param name="cypher">Cypher.</param>
        public async Task<JObject> ReceivePaymentRedemptionKey(string cypher)
        {
            Guard.Argument(fromAddress, nameof(fromAddress)).NotNull().NotEmpty();
            Guard.Argument(cypher, nameof(cypher)).NotNull().NotEmpty();

            bool TestFromAddress() => Util.FormatNetworkAddress(DecodeAddress(FromAddress()).ToArray()) != null;
            if (TestFromAddress().Equals(false))
            {
                return JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to read the recipient public key!"
                });
            }

            await SetSecretKey();

            var pk = Util.FormatNetworkAddress(DecodeAddress(FromAddress()).ToArray());
            var notification = JObject.Parse(cypher).ToObject<NotificationDto>();
            var message = ReadMessage(notification.Body, pk);
            var (isPayment, store) = ParseMessage(message);
            var previousBal = await CheckBalance();
            var payment = await Payment(store);

            if (payment)
            {
                var availableBal = await CheckBalance();
                var lastAmount = await walletService.LastTransactionAmount(Identifier(), MasterKey(), TransactionType.Receive);
                return JObject.FromObject(new
                {
                    success = true,
                    message = new
                    {
                        previous = previousBal,
                        received = lastAmount,
                        available = availableBal
                    }
                });
            }

            return JObject.FromObject(new
            {
                success = false,
                message = new
                {
                    available = previousBal
                }
            });
        }

        /// <summary>
        /// Sync wallet.
        /// </summary>
        /// <returns>Wallet transactions</returns>
        public async Task<List<TransactionDto>> Sync()
        {
            var transactions = await walletService.Transactions(Identifier(), MasterKey());

            if (transactions == null)
            {
                UpdateMessagePump("Nothing to sync ...");
                return null;
            }

            var tasks = transactions.Select(tx => GetAsync<CoinDto>(tx.Hash, RestApiMethod.Coin));
            var results = await Task.WhenAll(tasks);
            var coins = results.Where(c => c != null).ToList();
            var newTransactions = new List<TransactionDto>();

            coins.ForEach((CoinDto coin) =>
            {
                coin = coin.FormatCoinFromBase64();
                newTransactions.Add(transactions.FirstOrDefault(x => x.Hash.Equals(coin.Hash)));
            });

            UpdateMessagePump("Synced wallet ...");

            return newTransactions;
        }

        /// <summary>
        /// Checks the notifications.
        /// </summary>
        /// <returns>The notifications.</returns>
        /// <param name="address">Address.</param>
        /// <param name="notifications">Notifications.</param>
        /// <param name="pk">Pk.</param>
        private async Task CheckNotifications(string address, IEnumerable<NotificationDto> notifications, byte[] pk)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();
            Guard.Argument(notifications, nameof(notifications)).NotNull("Failed to get notification messages.");
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            int skip = 1;
            var take = notifications.Count();

            foreach (var notification in notifications)
            {
                var message = ReadMessage(notification.Body, pk);
                var (isPayment, store) = ParseMessage(message);

                if (!isPayment)
                {
                    var sharedKey = ToSharedKey(DecodeAddress(store).ToArray());
                    var notificationAddress = EncodeAddress(Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk).ToHex());
                    var decode = DecodeAddress(address).ToArray();

                    await ReceivePayment(notificationAddress, true, decode);
                    break;
                }

                UpdateMessagePump($"Processing payment {skip} of {take} ...");

                var payment = await Payment(store);

                if (payment)
                {
                    var track = new MessageTrackDto
                    {
                        PublicKey = pk.ToHex(),
                        Skip = skip,
                        Take = take
                    };

                    //TODO: Could possibility fail.. need recovery..
                    var added = await walletService.AddMessageTracking(Identifier(), MasterKey(), track);
                }

                skip++;
            }
        }

        /// <summary>
        /// Payment.
        /// </summary>
        /// <returns>The payment.</returns>
        /// <param name="message">Message.</param>
        private async Task<bool> Payment(string message)
        {
            Guard.Argument(message, nameof(message)).NotNull().NotEmpty();

            var redemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(message);
            var coin = await GetAsync<CoinDto>(redemptionKey.Hash, RestApiMethod.Coin);

            if (coin == null)
                return false;

            return await Payment(redemptionKey, coin);
        }

        public async Task<bool> Payment(RedemptionKeyDto redemptionKey, CoinDto coin)
        {
            Guard.Argument(coin, nameof(coin)).NotNull();
            Guard.Argument(redemptionKey, nameof(redemptionKey)).NotNull();

            try
            {
                var (swap1, swap2) = coinService.CoinSwap(MasterKey(), coin, redemptionKey);

                var keeperPass = await CoinPass(swap1, 3);
                if (keeperPass.Equals(false))
                    return false;

                //TODO: Above coin swap passes which writes to the ledger.. full pass could fail.. need recovery..
                var fullPass = await CoinPass(swap2, 1);
                if (fullPass.Equals(false))
                    return false;

                //TODO: Could possibility fail.. need recovery..
                var added = await AddWalletTransaction(coin, redemptionKey.Amount, redemptionKey.Memo, redemptionKey.Blind.FromHex(), TransactionType.Receive);
                if (added.Equals(false))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return false;
        }

        /// <summary>
        ///  Checks if the coin equals the mode.
        /// </summary>
        /// <returns>The pass.</returns>
        /// <param name="swap">Swap.</param>
        /// <param name="mode">Mode.</param>
        private async Task<bool> CoinPass(CoinDto swap, int mode)
        {
            Guard.Argument(swap, nameof(swap)).NotNull();
            Guard.Argument(mode, nameof(mode)).NotNegative();

            var canPass = false;
            var coin = coinService.DeriveCoin(swap, MasterKey());
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
        /// Gets the secret key.
        /// </summary>
        /// <returns>The key.</returns>
        public SecureString SecretKey() => secretKey;

        /// <summary>
        /// Secrets the key.
        /// </summary>
        /// <returns>The key.</returns>
        /// <param name="sk">Sk.</param>
        public ActorService SecretKey(SecureString sk)
        {
            secretKey = Guard.Argument(sk, nameof(sk)).NotNull("Either you have used the wrong address or vault is sealed!");
            return this;
        }

        /// <summary>
        /// Establishes first time public key message.
        /// </summary>
        /// <returns>The pub key message.</returns>
        public Task<MessageDto> EstablishPubKeyMessage()
        {
            MessageDto msg = null;

            try
            {
                var pk = Util.FormatNetworkAddress(DecodeAddress(ToAddress()).ToArray());
                var notificationAddress = Cryptography.GenericHashWithKey(pk.ToHex(), pk);
                var senderPk = PublicKey().ToUnSecureString();
                var innerMessage = JObject.FromObject(new
                {
                    payment = false,
                    store = EncodeAddress(senderPk)
                });
                var paddedBuf = Cryptography.Pad(innerMessage.ToString());
                var cypher = Cypher(Encoding.UTF8.GetString(paddedBuf), pk);
                var payload = new MessageDto
                {
                    Address = notificationAddress.ToBase64(),
                    Body = cypher.ToBase64()
                };

                msg = Util.TriesUntilCompleted<MessageDto>(
                     async () => { return await AddAsync(payload, RestApiMethod.PostMessage); }, 10, 100, payload).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                });
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return Task.FromResult(msg);
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

        //TODO Needs refactoring..
        /// <summary>
        /// Sends the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        public async Task<bool> SendPayment()
        {
            bool TestToAddress() => Util.FormatNetworkAddress(DecodeAddress(ToAddress()).ToArray()) != null;
            if (TestToAddress().Equals(false))
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to read the recipient public key!"
                });
                return false;
            }

            var isSpendable = await Spendable();
            if (isSpendable.Equals(false))
                return false;

            await SetRandomAddress();
            await SetSecretKey();
            await SetPublicKey();

            var spend = await Util.TriesUntilCompleted<CoinDto>(async () => { return await Spend(); }, 10, 100);
            if (spend == null)
                return false;

            var receiver = await Util.TriesUntilCompleted<bool>(async () => { return await SendReceiverCoin(); }, 10, 100, true);
            if (receiver.Equals(false))
                return false;

            return true;
        }

        /// <summary>
        /// Sets random address.
        /// </summary>
        /// <returns>The random address.</returns>
        public async Task SetRandomAddress()
        {
            FromAddress(await walletService.RandomAddress(Identifier(), MasterKey()));
        }

        /// <summary>
        /// Gets the specified To address.
        /// </summary>
        /// <returns>The to.</returns>
        public string ToAddress() => toAdress;

        /// <summary>
        /// Set the specified To address.
        /// </summary>
        /// <returns>The to.</returns>
        /// <param name="address">Address.</param>
        public ActorService ToAddress(string address)
        {
            toAdress = Guard.Argument(address, nameof(address)).NotNull().NotEmpty();
            return this;
        }

        /// <summary>
        /// Gets the specified from address.
        /// </summary>
        /// <returns>The fromAddress.</returns>
        public string FromAddress() => fromAddress;

        /// <summary>
        /// Set the specified from address.
        /// </summary>
        /// <returns>The fromAddress.</returns>
        /// <param name="address">Address.</param>
        public ActorService FromAddress(string address)
        {
            fromAddress = Guard.Argument(address, nameof(address)).NotNull().NotEmpty();
            return this;
        }

        /// <summary>
        /// Gets the last error.
        /// </summary>
        /// <returns>The last error.</returns>
        public JObject GetLastError() => lastError;

        //TODO: Redemption keys could possibility fail.. need recovery..
        /// <summary>
        /// Sends the payment message.
        /// </summary>
        /// <returns>The payment message.</returns>
        /// <param name="send">If set to <c>true</c> send.</param>
        public async Task<JObject> SendPaymentMessage(bool send)
        {
            var message = BuildRedemptionKeyMessage();

            if (message == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = lastError
                });

            if (send)
            {
                UpdateMessagePump("Sending redemption key ...");
                return await SendMessage(message);
            }

            return JObject.FromObject(new
            {
                success = true,
                message
            });
        }

        /// <summary>
        /// Sets the secret key.
        /// </summary>
        /// <returns>The secret key.</returns>
        public async Task SetSecretKey()
        {
            SecretKey(await walletService.StoreKey(Identifier(), MasterKey(), StoreKeyApiMethod.SecretKey, FromAddress()));
        }

        /// <summary>
        /// Sets the public key.
        /// </summary>
        /// <returns>The public key.</returns>
        public async Task SetPublicKey()
        {
            PublicKey(await walletService.StoreKey(Identifier(), MasterKey(), StoreKeyApiMethod.PublicKey, FromAddress()));
        }

        /// <summary>
        /// Builds the redemption key message.
        /// </summary>
        /// <returns>The redemption key message.</returns>
        private MessageDto BuildRedemptionKeyMessage()
        {
            MessageDto message = null;

            try
            {
                var (key1, key2) = coinService.HotRelease(coinService.Coin().Version, coinService.Coin().Stamp, MasterKey());
                var redemption = new RedemptionKeyDto
                {
                    Amount = coinService.TransactionCoin().Input,
                    Blind = coinService.TransactionCoin().Blind,
                    Hash = coinService.Coin().Hash,
                    Key1 = key1,
                    Key2 = key2,
                    Memo = Memo(),
                    Stamp = coinService.Coin().Stamp
                };
                var innerMessage = JObject.FromObject(new
                {
                    payment = true,
                    store = JsonConvert.SerializeObject(redemption)
                });
                var paddedBuf = Cryptography.Pad(innerMessage.ToString());
                var pk = Util.FormatNetworkAddress(DecodeAddress(ToAddress()).ToArray());
                var cypher = Cypher(Encoding.UTF8.GetString(paddedBuf), pk);
                var sharedKey = ToSharedKey(pk.ToArray());
                var notificationAddress = Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk);

                message = new MessageDto
                {
                    Address = notificationAddress.ToBase64(),
                    Body = cypher.ToBase64()
                };

                coinService.ClearCache();
            }
            catch (Exception ex)
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                });
                logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return message;
        }

        /// <summary>
        /// Spend.
        /// </summary>
        /// <returns>The spend.</returns>
        private async Task<CoinDto> Spend()
        {
            CoinDto coin = null;
            var transactionCoin = await walletService.SortChange(Identifier(), MasterKey(), Amount());

            if (transactionCoin == null)
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = "Not enough coin on a sigle chain for the request!"
                });
                return null;
            }

            var senderCoin = coinService
                             .TransactionCoin(transactionCoin)
                             .BuildSender(MasterKey())
                             .Coin();

            if (senderCoin == null)
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to build sender coin!"
                });
                return null;
            }

            senderCoin.Network = walletService.NetworkAddress(senderCoin).ToHex();

            coin = await AddAsync(senderCoin.FormatCoinToBase64(), RestApiMethod.PostCoin);

            if (coin == null)
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = "Failed to send the request!"
                });
                return null;
            }

            //TODO: Need to update node side..
            coin.Network = walletService.NetworkAddress(coin).ToBase64();

            //TODO: Could possibility fail.. need recovery..
            var added = await AddWalletTransaction(coin, coinService.TransactionCoin().Input, Memo(), null, TransactionType.Send);

            return coin;
        }

        /// <summary>
        /// Add the Wallet transaction.
        /// </summary>
        /// <returns>The Wallet transaction.</returns>
        /// <param name="coin">Coin.</param>
        /// <param name="transactionType">Transaction type.</param>
        private async Task<bool> AddWalletTransaction(CoinDto coin, ulong total, string memoText, byte[] blind, TransactionType transactionType)
        {
            Guard.Argument(coin, nameof(coin)).NotNull();
            Guard.Argument(total, nameof(total)).NotNegative();

            CoinDto formattedCoin = null;

            try
            { formattedCoin = coin.FormatCoinFromBase64(); }
            catch (FormatException)
            { formattedCoin = coin; }

            var transaction = new TransactionDto
            {
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

            var added = await walletService.AddTransaction(Identifier(), MasterKey(), transaction);

            return added;
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message">Message.</param>
        private async Task<JObject> SendMessage(MessageDto message)
        {
            Guard.Argument(message, nameof(message)).NotNull();

            var msg = await EstablishPubKeyMessage();

            if (msg == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = lastError
                });

            await Task.Delay(500);

            msg = await Util.TriesUntilCompleted<MessageDto>(
                async () => { return await AddAsync(message, RestApiMethod.PostMessage); }, 10, 100, message);

            if (msg == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = "Payment message failed to send!"
                });

            return JObject.FromObject(new
            {
                success = true,
                message = "Message sent."
            });
        }

        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="body">Body.</param>
        /// <param name="pk">Pk.</param>
        private string ReadMessage(string body, byte[] pk)
        {
            Guard.Argument(body, nameof(body)).NotNull().NotEmpty();
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);

            string unpadded = null;

            using (var insecureSk = SecretKey().Insecure())
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

        /// <summary>
        /// Spendable.
        /// </summary>
        /// <returns>The spendable.</returns>
        private async Task<bool> Spendable()
        {
            bool canSpend;
            var balance = await CheckBalance();

            if (balance >= Amount())
                canSpend = true;
            else
            {
                lastError = JObject.FromObject(new
                {
                    success = false,
                    message = new
                    {
                        available = balance,
                        spend = Amount()
                    }
                });

                canSpend = false;
            }

            return canSpend;
        }

        /// <summary>
        /// Sends the receiver coin.
        /// </summary>
        /// <returns>The receiver coin.</returns>
        private async Task<bool> SendReceiverCoin()
        {
            bool sent;

            UpdateMessagePump("Sending receiver coin ...");

            var coin = coinService.BuildReceiver(MasterKey()).Coin();

            coin.Network = walletService.NetworkAddress(coin).ToHex();
            coin = await AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin);

            switch (coin)
            {
                case null:
                    lastError = JObject.FromObject(new
                    {
                        success = false,
                        message = "Failed to post receiver coin!"
                    });
                    sent = false;
                    break;
                default:
                    sent = true;
                    break;
            }

            return sent;
        }

        /// <summary>
        /// Resends the receiver coin.
        /// </summary>
        /// <returns>The send receiver coin.</returns>
        /// <param name="count">Count.</param>
        private async Task<bool> ReSendReceiverCoin(int count = 0)
        {
            var sent = await SendReceiverCoin();

            if (sent.Equals(true))
            {
                count = 3;
                return true;
            }

            if (!count.Equals(3))
            {
                count++;
                await ReSendReceiverCoin(count);
            }

            return sent;
        }
    }
}