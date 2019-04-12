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
using Cypher.ApplicationLayer.Onion;
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

namespace TangramCypher.ApplicationLayer.Actor
{
    public class ActorService : IActorService
    {
        protected SecureString masterKey;
        protected string toAdress;
        protected double amount;
        protected string memo;
        protected double change;
        protected SecureString secretKey;
        protected SecureString publicKey;
        protected SecureString identifier;

        private readonly IConfigurationSection apiRestSection;
        private readonly ILogger logger;
        private readonly IOnionService onionService;
        private readonly IWalletService walletService;
        private readonly ICoinService coinService;
        private readonly Client client;

        public event MessagePumpEventHandler MessagePump;
        protected void OnMessagePump(MessagePumpEventArgs e) => MessagePump?.Invoke(this, e);

        public ActorService(IOnionService onionService, IWalletService walletService, ICoinService coinService, IConfiguration configuration, ILogger logger)
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
            if (payload.Equals(null))
                throw new ArgumentNullException(nameof(payload));

            var baseAddress = GetBaseAddress();
            var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString());
            var jObject = await client.PostAsync(payload, baseAddress, path, new CancellationToken());

            onionService.ChangeCircuit("ILoveTangram".ToSecureString());

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
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address is missing!", nameof(address));

            var baseAddress = GetBaseAddress();
            var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address);
            var jObject = await client.GetAsync<T>(baseAddress, path, new CancellationToken());

            onionService.ChangeCircuit("ILoveTangram".ToSecureString());

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
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address is missing!", nameof(address));

            var baseAddress = GetBaseAddress();
            var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address, skip, take);
            var returnMessages = await client.GetRangeAsync(baseAddress, path, new CancellationToken());
            var messages = returnMessages.Select(m => m.ToObject<T>());

            onionService.ChangeCircuit("ILoveTangram".ToSecureString());

            return Task.FromResult(messages).Result;
        }

        /// <summary>
        /// Gets the Amount instance.
        /// </summary>
        /// <returns>The amount.</returns>
        public double Amount() => amount;

        /// <summary>
        /// Sets the specified Amount value.
        /// </summary>
        /// <returns>The amount.</returns>
        /// <param name="value">Value.</param>
        public ActorService Amount(double value)
        {
            if (value < 0)
                throw new Exception("Value can not be less than zero!");

            amount = Math.Abs(value);

            return this;
        }

        /// <summary>
        /// Gets the change.
        /// </summary>
        /// <returns>The change.</returns>
        public double GetChange() => change;

        /// <summary>
        /// Checks the balance.
        /// </summary>
        /// <returns>The balance.</returns>
        public async Task<double> CheckBalance() => await walletService.AvailableBalance(Identifier(), From());

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
        public SecureString From() => masterKey;

        /// <summary>
        /// Sets the specified password.
        /// </summary>
        /// <returns>The from.</returns>
        /// <param name="password">Password.</param>
        public ActorService From(SecureString password)
        {
            masterKey = password ?? throw new ArgumentNullException(nameof(masterKey));
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
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("message", nameof(message));

            if ((pk == null) && (pk.Length > 32))
                throw new ArgumentNullException(nameof(pk));

            return Cryptography.BoxSeal(Utilities.BinaryToHex(Encoding.UTF8.GetBytes(message)), pk);
        }

        /// <summary>
        /// Gets the shared key.
        /// </summary>
        /// <returns>The shared key.</returns>
        /// <param name="pk">Pk.</param>
        public async Task<byte[]> ToSharedKey(byte[] pk)
        {
            if ((pk == null) && (pk.Length > 32))
                throw new ArgumentNullException(nameof(pk));

            await SetSecretKey();

            using (var insecure = SecretKey().Insecure())
            {
                return Cryptography.ScalarMult(Utilities.HexToBinary(insecure.Value), pk);
            }
        }

        /// <summary>
        /// Gets the walletId instance.
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
            identifier = walletId ?? throw new ArgumentNullException(nameof(walletId));
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
        /// <param name="text">Text.</param>
        public ActorService Memo(string text)
        {
            if (string.IsNullOrEmpty(text))
                memo = string.Empty;

            if (text.Length > 64)
                throw new Exception("Memo field cannot be more than 64 characters long!");

            memo = text;

            return this;
        }

        /// <summary>
        /// Opens the box seal.
        /// </summary>
        /// <returns>The box seal.</returns>
        /// <param name="cypher">Cypher.</param>
        /// <param name="pkSkDto">Pk sk dto.</param>
        public string OpenBoxSeal(string cypher, PkSkDto pkSkDto)
        {
            if (string.IsNullOrEmpty(cypher))
                throw new ArgumentException("Cypher is missing!", nameof(cypher));

            if (pkSkDto == null)
                throw new ArgumentNullException(nameof(pkSkDto));

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
            publicKey = pk ?? throw new ArgumentNullException(nameof(pk));
            return this;
        }

        /// <summary>
        /// Receives the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        /// <param name="address">Address.</param>
        public async Task ReceivePayment(string address, bool sharedKey = false, byte[] receiverPk = null)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("message", nameof(address));

            IEnumerable<NotificationDto> notifications;
            var notificationAddress = string.Empty;
            var pk = Util.FormatNetworkAddress(DecodeAddress(address).ToArray());

            notificationAddress = sharedKey ? pk.ToHex() : Cryptography.GenericHashWithKey(pk.ToHex(), pk).ToHex();

            var messageTrack = await walletService.MessageTrack(Identifier(), From(), pk.ToHex());

            UpdateMessagePump("Fetching messages ...");

            var count = await GetAsync<JObject>(notificationAddress, RestApiMethod.MessageCount);
            int countValue = count == null ? 1 : count.Value<int>("count");

            notifications = messageTrack == null
                ? await GetRangeAsync<NotificationDto>(notificationAddress, 0, countValue, RestApiMethod.MessageRange)
                : await GetRangeAsync<NotificationDto>(notificationAddress, messageTrack.Skip, messageTrack.Take, RestApiMethod.MessageRange);

            if (sharedKey)
                pk = Util.FormatNetworkAddress(receiverPk);

            await CheckNotifications(address, notifications, pk);
        }

        /// <summary>
        /// Receives payment from redemption key.
        /// </summary>
        /// <returns>The payment redemption key.</returns>
        /// <param name="address">Address.</param>
        /// <param name="cypher">Cypher.</param>
        public async Task<JObject> ReceivePaymentRedemptionKey(string address, string cypher)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("message", nameof(address));

            if (string.IsNullOrEmpty(cypher))
                throw new ArgumentException("message", nameof(cypher));

            var pk = DecodeAddress(address).ToArray();
            var notification = JObject.Parse(cypher).ToObject<NotificationDto>();
            var message = await ReadMessage(notification.Body, pk);
            var (isPayment, store) = ParseMessage(message);
            var previousBal = await CheckBalance();
            var payment = await Payment(store);

            if (payment)
            {
                var availableBal = await CheckBalance();
                return JObject.FromObject(new
                {
                    success = true,
                    message = new
                    {
                        previous = previousBal,
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
        /// Checks the notifications.
        /// </summary>
        /// <returns>The notifications.</returns>
        /// <param name="address">Address.</param>
        /// <param name="notifications">Notifications.</param>
        /// <param name="pk">Pk.</param>
        private async Task CheckNotifications(string address, IEnumerable<NotificationDto> notifications, byte[] pk)
        {
            int skip = 1;
            var take = notifications.Count();

            foreach (var notification in notifications)
            {
                var message = await ReadMessage(notification.Body, pk);
                var (isPayment, store) = ParseMessage(message);

                if (!isPayment)
                {
                    var sharedKey = await ToSharedKey(DecodeAddress(store).ToArray());
                    var notificationAddress = EncodeAddress(Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk).ToHex());
                    var decode = DecodeAddress(address).ToArray();

                    await ReceivePayment(notificationAddress, true, decode);

                    break;
                }

                UpdateMessagePump($"Checking payment {skip} of {take} ...");

                var payment = await Payment(store);

                if (payment)
                {
                    var track = new MessageTrackDto
                    {
                        PublicKey = pk.ToHex(),
                        Skip = skip,
                        Take = take
                    };

                    skip++;

                    await walletService.AddMessageTracking(Identifier(), From(), track);
                }
            }
        }

        /// <summary>
        /// Payment.
        /// </summary>
        /// <returns>The payment.</returns>
        /// <param name="message">Message.</param>
        private async Task<bool> Payment(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("message", nameof(message));

            var freeRedemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(message);
            var coin = await GetAsync<CoinDto>(freeRedemptionKey.Hash, RestApiMethod.Coin);

            if (coin != null)
            {
                try
                {
                    coin = coin.FormatCoinFromBase64();

                    var (swap1, swap2) = coinService.CoinSwap(From(), coin, freeRedemptionKey);

                    var keeperPass = await CoinPass(swap1, 3);
                    if (keeperPass != true)
                        return false;

                    var fullPass = await CoinPass(swap2, 1);
                    if (fullPass != true)
                        return false;

                    await walletService.AddTransaction(Identifier(), From(),
                          new TransactionDto
                          {
                              Amount = freeRedemptionKey.Amount,
                              Commitment = coin.Envelope.Commitment,
                              Hash = swap2.Hash,
                              Stamp = swap2.Stamp,
                              Version = swap2.Version,
                              TransactionType = TransactionType.Receive
                          });

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
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
            if (swap == null)
                throw new ArgumentNullException(nameof(swap));

            var canPass = false;
            var coin = coinService.DeriveCoin(From(), swap);
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
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("message", nameof(message));

            var jObject = JObject.Parse(message);

            return (jObject.Value<bool>("payment"), jObject.Value<string>("store"));
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
            secretKey = sk ?? throw new ArgumentNullException(nameof(sk));
            return this;
        }

        /// <summary>
        /// Establishes first time public key message.
        /// </summary>
        /// <returns>The pub key message.</returns>
        public async Task<MessageDto> EstablishPubKeyMessage()
        {
            await SetPublicKey();

            var pk = Util.FormatNetworkAddress(DecodeAddress(To()).ToArray());
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
            var message = await AddAsync(payload, RestApiMethod.PostMessage);

            return message;
        }

        /// <summary>
        /// Encodes the address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="pk">Pk.</param>
        private static string EncodeAddress(string pk)
        {
            return Base58.Bitcoin.Encode(Utilities.HexToBinary(pk));
        }

        ///TODO: Clean up code..
        /// <summary>
        /// Sends the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        public async Task<JObject> SendPayment(bool sendMessage)
        {
            var bal = await CheckBalance();

            if (bal < Amount())
                return JObject.FromObject(new
                {
                    success = false,
                    message = new
                    {
                        available = bal,
                        spend = Amount()
                    }
                });

            await SetSecretKey();

            var spendCoins = await GetCoinsToSpend();
            var coins = await PostCoinsAsync(spendCoins);

            if (coins == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = "Coins failed to post!"
                });

            await AddWalletTransactions(coins);

            UpdateMessagePump("Sending coin ...");

            var (receiverOutput, receiverCoin) = coinService.BuildReceiver();

            coinService.ClearCache();

            receiverCoin.Network = walletService.NetworkAddress(receiverCoin).ToHex();
            receiverCoin = await AddAsync(receiverCoin.FormatCoinToBase64(), RestApiMethod.PostCoin);

            if (receiverCoin == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = "Receiver coin failed!"
                });

            var message = await BuildRedemptionKeyMessage(receiverOutput, receiverCoin.FormatCoinFromBase64());

            if (sendMessage)
                return await SendMessage(message);

            return JObject.Parse(JsonConvert.SerializeObject(message));
        }

        /// <summary>
        /// Gets the specified To address.
        /// </summary>
        /// <returns>The to.</returns>
        public string To() => toAdress;

        /// <summary>
        /// Set the specified To address.
        /// </summary>
        /// <returns>The to.</returns>
        /// <param name="address">Address.</param>
        public ActorService To(string address)
        {
            if (string.IsNullOrEmpty(address))
                throw new Exception("To address is missing!");

            toAdress = address;

            return this;
        }

        /// <summary>
        /// Sets the secret key.
        /// </summary>
        /// <returns>The secret key.</returns>
        private async Task SetSecretKey()
        {
            try
            {
                SecretKey(await walletService.StoreKey(Identifier(), From(), "SecretKey"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Sets the public key.
        /// </summary>
        /// <returns>The public key.</returns>
        private async Task SetPublicKey()
        {
            try
            {
                PublicKey(await walletService.StoreKey(Identifier(), From(), "PublicKey"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Builds the redemption key message.
        /// </summary>
        /// <returns>The redemption key message.</returns>
        /// <param name="coin">Coin.</param>
        private async Task<MessageDto> BuildRedemptionKeyMessage(ReceiverOutput receiverOutput, CoinDto coin)
        {
            if (receiverOutput == null)
                throw new ArgumentNullException(nameof(receiverOutput));

            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            var (key1, key2) = coinService.HotRelease(coin.Version, coin.Stamp, From());
            var redemption = new RedemptionKeyDto
            {
                Amount = receiverOutput.Amount,
                Blind = receiverOutput.Blind.ToHex(),
                Hash = coin.Hash,
                Key1 = key1,
                Key2 = key2,
                Memo = Memo(),
                Stamp = coin.Stamp
            };
            var innerMessage = JObject.FromObject(new
            {
                payment = true,
                store = JsonConvert.SerializeObject(redemption)
            });
            var paddedBuf = Cryptography.Pad(innerMessage.ToString());
            var pk = Util.FormatNetworkAddress(DecodeAddress(To()).ToArray());
            var cypher = Cypher(Encoding.UTF8.GetString(paddedBuf), pk);
            var sharedKey = await ToSharedKey(pk.ToArray());
            var notificationAddress = Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk);
            var message = new MessageDto
            {
                Address = notificationAddress.ToBase64(),
                Body = cypher.ToBase64()
            };

            return message;
        }


        /// TODO: Now operating on a single coin as the output will create a new block. 
        /// We need to handle mutiple coins...

        /// <summary>
        /// Gets the coins to spend.
        /// </summary>
        /// <returns>The coins to spend.</returns>
        private async Task<IEnumerable<CoinDto>> GetCoinsToSpend()
        {
            CoinDto coin = null;
            var makeChange = await walletService.MakeChange(Identifier(), From(), Amount());

            if ((makeChange != null) && (makeChange.Transaction != null))
            {
                coin = coinService
                  .Password(From())
                  .Input(makeChange.Transaction.Amount)
                  .Output(makeChange.AmountFor)
                  .Stamp(makeChange.Transaction.Stamp)
                  .Version(makeChange.Transaction.Version)
                  .BuildSender();
            }

            coin.Network = walletService.NetworkAddress(coin).ToHex();

            return new List<CoinDto> { coin };
        }

        /// <summary>
        /// Adds the wallet transactions.
        /// </summary>
        /// <returns>The wallet transactions.</returns>
        /// <param name="coins">Coins.</param>
        private async Task AddWalletTransactions(IEnumerable<CoinDto> coins)
        {
            List<Task> tasks = new List<Task>();

            foreach (var coin in coins)
            {
                var formattedCoin = coin.FormatCoinFromBase64();
                var sumAmount = Math.Abs(await walletService.TransactionAmount(Identifier(), From(), formattedCoin.Stamp)) - Math.Abs(Amount());
                var isAmount = sumAmount.Equals(coinService.Change()) ? sumAmount : coinService.Change();

                var transaction = new TransactionDto
                {
                    Amount = isAmount,
                    Commitment = formattedCoin.Envelope.Commitment,
                    Hash = formattedCoin.Hash,
                    Stamp = formattedCoin.Stamp,
                    Version = formattedCoin.Version,
                    TransactionType = TransactionType.Send
                };

                tasks.Add(walletService.AddTransaction(Identifier(), From(), transaction));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Posts the coins.
        /// </summary>
        /// <returns>The coins.</returns>
        /// <param name="coins">Coins.</param>
        private async Task<IEnumerable<CoinDto>> PostCoinsAsync(IEnumerable<CoinDto> coins)
        {
            var tasks = coins.Select(coin => AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin));
            var results = await Task.WhenAll(tasks);
            var json = await results.AsJson().ReadAsStringAsync();

            if (json.Equals("[null]") || json == null) return null;

            return JToken.Parse(json).ToObject<IEnumerable<CoinDto>>();
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message">Message.</param>
        private async Task<JObject> SendMessage(MessageDto message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var msg = await EstablishPubKeyMessage();

            if (msg == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = "First message failed to send!"
                });

            await Task.Delay(500);

            msg = await AddAsync(message, RestApiMethod.PostMessage);

            if (msg == null)
                return JObject.FromObject(new
                {
                    success = false,
                    message = "Second message failed to send!"
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
        private async Task<string> ReadMessage(string body, byte[] pk)
        {
            if (string.IsNullOrEmpty(body))
                throw new ArgumentException("message", nameof(body));

            if ((pk == null) && (pk.Length > 32))
                throw new ArgumentNullException(nameof(pk));

            await SetSecretKey();

            using (var insecureSk = SecretKey().Insecure())
            {
                var message = Utilities.HexToBinary(Encoding.UTF8.GetString(Convert.FromBase64String(body)));
                var opened = Cryptography.OpenBoxSeal(message, new KeyPair(pk, Utilities.HexToBinary(insecureSk.Value)));
                var unpadded = Cryptography.Unpad(opened.FromHex());

                return Encoding.UTF8.GetString(unpadded);
            }
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
            OnMessagePump(new MessagePumpEventArgs { Message = message });
            Task.Delay(500);
        }
    }
}