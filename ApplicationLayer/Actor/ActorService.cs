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
using TangramCypher.ApplicationLayer.Helper.ZeroKP;
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
        protected double? amount;
        protected string memo;
        protected double? change;
        protected SecureString secretKey;
        protected SecureString publicKey;
        protected SecureString identifier;

        private readonly IConfigurationSection apiRestSection;
        private readonly IConfigurationSection apiOnionSection;
        private readonly ILogger logger;
        private readonly IOnionService onionService;
        private readonly IWalletService walletService;
        private readonly ICoinService coinService;

        public ActorService(IOnionService onionService, IWalletService walletService, ICoinService coinService, IConfiguration configuration, ILogger logger)
        {
            this.onionService = onionService;
            this.walletService = walletService;
            this.coinService = coinService;
            this.logger = logger;

            apiRestSection = configuration.GetSection(Constant.ApiGateway);
            apiOnionSection = configuration.GetSection(Constant.Onion);

            //var pass = "the few depth squeaked animist ones relabels a sadistic gap".ToSecureString();
            //var id = "id_04ec58c0c32b0dc517f54516b2b17f59".ToSecureString();
            //var h = "7da16ee30a273db6b04a9e05dcdeed229f63e03cabc856de48726f6b9f1a9ac4";

            //var coin = coinService
            //      .Password(pass)
            //      .Input(5)
            //      .Output(2)
            //      .Stamp(GetStamp())
            //      .Version(1)
            //      .Build();

            //change = coinService.Change();

            //// var result = AddCoinAsync(coin.FormatCoinToBase64(), new CancellationToken()).GetAwaiter().GetResult();

            //coin = coinService
            //      .Password(pass)
            //      .Input(GetChange())
            //      .Output(0){"request_id":"0c76ec31-472d-a748-4cb2-640714a952de","lease_id":"","renewable":false,"lease_duration":2764800,"data":{"storeKeys":{"Address":"GLpSn2pX2Xyv5FwbyRMrJJF2HRVHZ88q9AGEMhskssMd","PublicKey":"e3f2fc1ed1c1bfb7df5f81fc8383e195a54c37eaa8576d3b3668eecc442dbf74","SecretKey":"83557fe14469adf59a620f2c5b8d350790ef9b6ffc8dd95b00792d55f66d1094"},"transactions":[{"Amount":3.0,"Commitment":"0873e72f13c8910589945f264060e9952b271ce750f9c3826240376e7c2ada2d3d","Hash":"7da16ee30a273db6b04a9e05dcdeed229f63e03cabc856de48726f6b9f1a9ac4","Stamp":"cf40359d6a24c4383d73131831c8884dca59ff9d384673c700a60e0578f4f083","Version":1}]},"wrap_info":null,"warnings":null,"auth":null}
            //      .Stamp(GetStamp())
            //      .Version(1)
            //      .Build();

            //// result = AddCoinAsync(coin.FormatCoinToBase64(), new CancellationToken()).GetAwaiter().GetResult();


            // var result = GetCoinAsync(h, new CancellationToken()).GetAwaiter().GetResult();


            //From(pass);
            //Identifier(id);

            //result = result.FormatCoinFromBase64();

            //walletService.AddTransaction(Identifier(), From(), new TransactionDto
            //{
            //    Amount = change.Value,
            //    Commitment = result.Envelope.Commitment,
            //    Hash = result.Hash,
            //    Stamp = result.Stamp,
            //    Version = result.Version
            //});

        }

        /// <summary>
        /// Adds the message async.
        /// </summary>
        /// <returns>The message async.</returns>
        /// <param name="message">Message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<JObject> AddMessageAsync(MessageDto message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.PostMessage);

            JObject jObject = apiOnionSection.GetValue<int>(Constant.OnionEnabled) == 1
                ? await onionService.ClientPostAsync(message, baseAddress, path, cancellationToken)
                : await Client.PostAsync(message, baseAddress, path, cancellationToken);

            return jObject;
        }

        /// <summary>
        /// Adds the coin async.
        /// </summary>
        /// <returns>The coin async.</returns>
        /// <param name="coin">Coin.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<JObject> AddCoinAsync(CoinDto coin, CancellationToken cancellationToken)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));
            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.PostCoin);

            JObject jObject = apiOnionSection.GetValue<int>(Constant.OnionEnabled) == 1
                ? await onionService.ClientPostAsync(coin, baseAddress, path, cancellationToken)
                : await Client.PostAsync(coin, baseAddress, path, cancellationToken);

            return jObject;
        }

        /// <summary>
        /// Gets the Amount instance.
        /// </summary>
        /// <returns>The amount.</returns>
        public double? Amount() => amount;

        /// <summary>
        /// Sets the specified Amount value.
        /// </summary>
        /// <returns>The amount.</returns>
        /// <param name="value">Value.</param>
        public ActorService Amount(double? value)
        {
            if (value == null)
                throw new Exception("Value can not be null!");
            if (Math.Abs(value.GetValueOrDefault()) < 0)
                throw new Exception("Value can not be less than zero!");

            amount = Math.Abs(value.Value);

            return this;
        }

        public double? GetChange() => change;

        /// <summary>
        /// Checks the balance.
        /// </summary>
        /// <returns>The balance.</returns>
        public async Task<double> CheckBalance() => await walletService.GetBalance(Identifier(), From());

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
        /// Gets the cypher.
        /// </summary>
        /// <returns>The chiper.</returns>
        /// <param name="redemptionKey">Redemption key.</param>
        /// <param name="pk">Pk.</param>
        public byte[] GetCypher(string redemptionKey, byte[] pk)
        {
            return Cryptography.BoxSeal(Utilities.BinaryToHex(Encoding.UTF8.GetBytes(redemptionKey)), pk);
        }

        /// <summary>
        /// Gets the message async.
        /// </summary>
        /// <returns>The message async.</returns>
        /// <param name="address">Address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<NotificationDto> GetMessageAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address is missing!", nameof(address));

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.GetMessages), address);

            NotificationDto message = apiOnionSection.GetValue<int>(Constant.OnionEnabled) == 1
                ? await onionService.ClientGetAsync<NotificationDto>(baseAddress, path, cancellationToken)
                : await Client.GetAsync<NotificationDto>(baseAddress, path, cancellationToken);

            return message;
        }

        /// <summary>
        /// Gets the shared key.
        /// </summary>
        /// <returns>The shared key.</returns>
        /// <param name="pk">Pk.</param>
        public async Task<byte[]> ToSharedKey(byte[] pk)
        {
            await SetSecretKey();

            using (var insecure = SecretKey().Insecure())
            {
                return Cryptography.ScalarMult(Utilities.HexToBinary(insecure.Value), pk);
            }
        }

        /// <summary>
        /// Gets the coin async.
        /// </summary>
        /// <returns>The coin async.</returns>
        /// <param name="stamp">Stamp.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<CoinDto> GetCoinAsync(string stamp, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentException("Stamp is missing!", nameof(stamp));

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.GetCoin), stamp);

            CoinDto coin = apiOnionSection.GetValue<int>(Constant.OnionEnabled) == 1
                ? await onionService.ClientGetAsync<CoinDto>(baseAddress, path, cancellationToken)
                : await Client.GetAsync<CoinDto>(baseAddress, path, cancellationToken);

            return coin;
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
        /// <param name="notification">Notification.</param>
        public async Task ReceivePayment(string address, NotificationDto notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var pk = DecodeAddress(address).ToArray();

            await SetSecretKey();

            using (var insecureSk = SecretKey().Insecure())
            {
                var message = Convert.FromBase64String(notification.Body);
                var openMessage = Cryptography.OpenBoxSeal(Utilities.HexToBinary(Encoding.UTF8.GetString(message)),
                    new KeyPair(pk, Utilities.HexToBinary(insecureSk.Value)));

                openMessage = Encoding.UTF8.GetString(Utilities.HexToBinary(openMessage));

                var freeRedemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(openMessage);
                var coin = await GetCoinAsync(freeRedemptionKey.Stamp, new CancellationToken());
                // TODO: Add chain of responsibility..
                if (coin != null)
                {
                    coin = coin.FormatCoinFromBase64();

                    var swap = coinService.CoinSwap(From(), coin, freeRedemptionKey);

                    var coinSwap1 = swap.Item1;

                    var result = await AddCoinAsync(coinSwap1.FormatCoinToBase64(), new CancellationToken());

                    if (result == null)
                        return;

                    var coinSwap2 = swap.Item2;
                    result = await AddCoinAsync(coinSwap2.FormatCoinToBase64(), new CancellationToken());

                    if (result == null)
                        return;

                    var coin1 = coinService.DeriveCoin(From(), coinSwap1);
                    var status1 = coinService.VerifyCoin(coinSwap1, coin1);

                    if (status1.Equals(4))
                        return;

                    var coin2 = coinService.DeriveCoin(From(), coinSwap2);
                    var status2 = coinService.VerifyCoin(coinSwap2, coin2);

                    if (status2.Equals(1))
                    {
                        result = await AddCoinAsync(coin2.FormatCoinToBase64(), new CancellationToken());

                        if (result == null)
                            return;

                        await walletService.AddTransaction(Identifier(), From(),
                            new TransactionDto
                            {
                                Amount = 0,
                                Commitment = coin.Envelope.Commitment,
                                Hash = coin2.Hash,
                                Stamp = coin2.Stamp,
                                Version = coin2.Version
                            });
                    }
                }
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
            secretKey = sk ?? throw new ArgumentNullException(nameof(sk));
            return this;
        }

        /// <summary>
        /// Sends the payment.
        /// </summary>
        /// <returns>The payment.</returns>
        public async Task<JObject> SendPayment(bool sendMessage)
        {
            var bal = await CheckBalance();

            if (bal < Amount())
                return JObject.Parse(@"{success: false, message: { available:" + bal + ", spend:" + Amount() + "}}");

            await SetSecretKey();

            var spendCoins = await GetCoinsToSpend();


            var coins = await PostCoins(spendCoins);

            await AddWalletTransactions(coins);

            // TODO Return message(s)
            return null;
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
                SecretKey(await walletService.GetStoreKey(Identifier(), From(), "SecretKey"));
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
                PublicKey(await walletService.GetStoreKey(Identifier(), From(), "PublicKey"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the stamp.
        /// </summary>
        /// <returns>The stamp.</returns>
        private string GetStamp()
        {
            return Cryptography.GenericHashNoKey(Cryptography.RandomKey()).ToHex();
        }

        /// <summary>
        /// Returns provers password.
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="password">Password.</param>
        /// <param name="version">Version.</param>
        public string ProverPassword(SecureString password, int version)
        {
            using (var insecurePassword = password.Insecure())
            {
                var hash = Cryptography.GenericHashNoKey(string.Format("{0} {1}", version, insecurePassword.Value));
                return Prover.GetHashStringNumber(hash).ToByteArray().ToHex();
            }
        }

        /// <summary>
        /// Builds the redemption key message.
        /// </summary>
        /// <returns>The redemption key message.</returns>
        /// <param name="coin">Coin.</param>
        private async Task<MessageDto> BuildRedemptionKeyMessage(CoinDto coin)
        {
            var redemptionKey = coinService.HotRelease(coin.Version, coin.Stamp, Memo(), From());
            var pk = DecodeAddress(To()).ToArray();
            var cypher = GetCypher(redemptionKey, pk);
            var sharedKey = await ToSharedKey(pk.ToArray());
            var notificationAddress = Cryptography.GenericHashWithKey(sharedKey.ToHex(), pk);
            var message = new MessageDto()
            {
                Address = notificationAddress.ToBase64(),
                Body = cypher.ToBase64()
            };

            return message;
        }

        /// <summary>
        /// Gets the coins to spend.
        /// </summary>
        /// <returns>The coins to spend.</returns>
        private async Task<IEnumerable<CoinDto>> GetCoinsToSpend()
        {
            var makeChange = await walletService.MakeChange(Identifier(), From(), Amount().Value);

            makeChange.Transactions.Add(makeChange.Transaction);

            var coins = coinService.MakeMultipleCoins(makeChange.Transactions, From());

            return coins;
        }

        /// <summary>
        /// Adds the wallet transactions.
        /// </summary>
        /// <returns>The wallet transactions.</returns>
        /// <param name="coins">Coins.</param>
        private async Task AddWalletTransactions(IEnumerable<JObject> coins)
        {
            var tasks = coins.Select(c =>
            {
                var coin = c.ToObject<CoinDto>();
                var transaction = new TransactionDto
                {
                    Amount = 0,
                    Commitment = coin.Envelope.Commitment,
                    Hash = coin.Hash,
                    Stamp = coin.Stamp,
                    Version = coin.Version
                };
                return walletService.AddTransaction(Identifier(), From(), transaction);
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Posts the coins.
        /// </summary>
        /// <returns>The coins.</returns>
        /// <param name="coins">Coins.</param>
        private async Task<IEnumerable<JObject>> PostCoins(IEnumerable<CoinDto> coins)
        {
            var tasks = coins.Select(i => AddCoinAsync(i.FormatCoinToBase64(), new CancellationToken()));
            return await Task.WhenAll(tasks);
        }
    }
}