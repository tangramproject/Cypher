using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Actor
{
    public class ActorService : IActorService
    {
        protected SecureString masterKey;
        protected string _toAdress;
        protected double? _amount;
        protected string _memo;
        protected SecureString secretKey;
        protected SecureString publicKey;
        protected SecureString identifier;

        readonly IConfigurationSection apiRestSection;
        readonly ILogger logger;
        readonly ICryptography cryptography;
        readonly IOnionService onionService;
        readonly IWalletService walletService;


        public event ReceivedMessageEventHandler ReceivedMessage;

        protected virtual void OnReceivedMessage(ReceivedMessageEventArgs e)
        {
            ReceivedMessage?.Invoke(this, e);
        }

        public ActorService(ICryptography cryptography, IOnionService onionService, IWalletService walletService, IConfiguration configuration, ILogger logger)
        {
            this.cryptography = cryptography;
            this.onionService = onionService;
            this.walletService = walletService;
            this.logger = logger;

            apiRestSection = configuration.GetSection(Constant.ApiGateway);
        }

        // TODO: Temp fix until we get the onion client working.
        public async Task<JObject> AddMessageAsync(NotificationDto notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.PostMessage);
            var result = await ClientPostAsync(notification, baseAddress, path, cancellationToken);

            return result;
        }

        // TODO: Temp fix until we get the onion client working.
        public async Task<JObject> AddCoinAsync(CoinDto coin, CancellationToken cancellationToken)
        {
            if (coin == null)
            {
                throw new ArgumentNullException(nameof(coin));
            }

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.PostCoin);
            var result = await ClientPostAsync(coin, baseAddress, path, cancellationToken);

            return result;
        }

        public double? Amount()
        {
            return _amount;
        }

        public ActorService Amount(double? value)
        {
            if (value == null)
            {
                throw new Exception("Value can not be null!");
            }
            if (Math.Abs(value.GetValueOrDefault()) <= 0)
            {
                throw new Exception("Value can not be zero!");
            }

            _amount = value;

            return this;
        }

        public Span<byte> DecodeAddress(string key)
        {
            return Base58.Bitcoin.Decode(key);
        }

        public string DeriveKey(int version, string stamp, SecureString password, int bytes = 32)
        {
            if (string.IsNullOrEmpty(stamp))
            {
                throw new ArgumentException("Stamp cannot be null or empty!", nameof(stamp));
            }

            return cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", version, stamp, password), bytes).ToHex();
        }

        public string DeriveSerialKey(int version, double? amount, SecureString password, int bytes = 32)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            return
            cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", version, amount.Value.ToString(), masterKey), bytes).ToHex();
        }

        public EnvelopeDto DeriveEnvelope(SecureString password, int version, double? amount)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var v0 = +version;
            return new EnvelopeDto()
            {
                Amount = amount.Value,
                Serial = DeriveSerialKey(v0, amount, password)
            };
        }

        public CoinDto DeriveCoin(SecureString password, int version, EnvelopeDto envelope)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var stamp = cryptography.GenericHashNoKey(string.Format("{0}{1}", envelope.Amount.ToString(), envelope.Serial)).ToHex();
            var v0 = +version;
            var v1 = +version + 1;
            var v2 = +version + 2;

            var chronicle = new CoinDto()
            {
                Keeper = DeriveKey(v1, stamp, DeriveKey(v2, stamp, DeriveKey(v2, stamp, password).ToSecureString()).ToSecureString()),
                Version = v0,
                Principle = DeriveKey(v0, stamp, masterKey),
                Stamp = stamp,
                Envelope = envelope,
                Hint = DeriveKey(v1, stamp, DeriveKey(v1, stamp, password).ToSecureString())
            };

            return chronicle;
        }

        public SecureString From()
        {
            return masterKey;
        }

        public ActorService From(SecureString password)
        {
            masterKey = password ?? throw new ArgumentNullException(nameof(masterKey));
            return this;
        }

        public byte[] GetChiper(string redemptionKey, Span<byte> bobPk)
        {
            return cryptography.BoxSeal(redemptionKey, bobPk.ToArray());
        }

        // TODO: Temp fix until we get the onion client working.
        public async Task<NotificationDto> GetMessageAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Address is missing!", nameof(address));
            }

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.GetMessage), address);
            var result = await ClientGetAsync<NotificationDto>(baseAddress, path, cancellationToken);

            return result;
        }

        public byte[] GetSharedKey(Span<byte> bobPk)
        {
            SetSecretKey().GetAwaiter().GetResult();

            using (var insecure = SecretKey().Insecure())
            {
                return cryptography.ScalarMult(Utilities.HexToBinary(insecure.Value), bobPk.ToArray());
            }
        }

        // TODO: Temp fix until we get the onion client working.
        public async Task<CoinDto> GetCoinAsync(string stamp, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(stamp))
            {
                throw new ArgumentException("Stamp is missing!", nameof(stamp));
            }

            var baseAddress = new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
            var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(Constant.GetCoinStamp), stamp);
            var result = await ClientGetAsync<CoinDto>(baseAddress, path, cancellationToken);

            return result;

        }

        public string HotRelease(CoinDto coin)
        {
            if (coin == null)
            {
                throw new ArgumentNullException(nameof(CoinDto));
            }


            var subKey1 = DeriveKey(coin.Version + 1, coin.Stamp, From());
            var subKey2 = DeriveKey(coin.Version + 2, coin.Stamp, From());
            var redemption = new RedemptionKeyDto() { Key1 = subKey1, Key2 = subKey2, Memo = Memo(), Stamp = coin.Stamp };

            return JsonConvert.SerializeObject(redemption);
        }

        public SecureString Identifier()
        {
            return identifier;
        }

        public ActorService Identifier(SecureString walletId)
        {
            identifier = walletId ?? throw new ArgumentNullException(nameof(walletId));
            return this;
        }

        public string Memo()
        {
            return _memo;
        }

        public ActorService Memo(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _memo = string.Empty;
            }

            if (text.Length > 64)
            {
                throw new Exception("Memo field cannot be more than 64 characters long!");
            }

            _memo = text;

            return this;
        }

        public string OpenBoxSeal(string cipher, PkSkDto pkSkDto)
        {
            if (string.IsNullOrEmpty(cipher))
            {
                throw new ArgumentException("Cipher cannot be null or empty!", nameof(cipher));
            }

            if (pkSkDto == null)
            {
                throw new ArgumentNullException(nameof(pkSkDto));
            }

            var pk = Encoding.UTF8.GetBytes(pkSkDto.PublicKey);
            var sk = Encoding.UTF8.GetBytes(pkSkDto.SecretKey.ToUnSecureString());
            var cypher = Encoding.UTF8.GetBytes(cipher);
            var message = cryptography.OpenBoxSeal(cypher, new KeyPair(pk, sk));

            return message;
        }

        public string PartialRelease(CoinDto coin)
        {
            if (coin == null)
            {
                throw new ArgumentNullException(nameof(coin));
            }

            var subKey1 = DeriveKey(coin.Version + 1, coin.Stamp, From());
            var subKey2 = DeriveKey(coin.Version + 2, coin.Stamp, From()).ToSecureString();
            var mix = DeriveKey(coin.Version + 2, coin.Stamp, subKey2);
            var redemption = new RedemptionKeyDto() { Key1 = subKey1, Key2 = mix, Memo = Memo(), Stamp = coin.Stamp };

            return JsonConvert.SerializeObject(redemption);
        }

        public SecureString PublicKey()
        {
            return publicKey;
        }

        public ActorService PublicKey(SecureString pk)
        {
            publicKey = pk ?? throw new ArgumentNullException(nameof(pk));
            return this;
        }

        public void ReceivePayment(string redemptionKey)
        {
            if (string.IsNullOrEmpty(redemptionKey))
            {
                throw new ArgumentException("Redemption Key cannot be null or empty!", nameof(redemptionKey));
            }

            SetSecretKey().GetAwaiter();
            SetPublicKey().GetAwaiter();

            using (var insecurePk = PublicKey().Insecure())
            {
                using (var insecureSk = SecretKey().Insecure())
                {
                    var openMessage = cryptography.OpenBoxSeal(Convert.FromBase64String(redemptionKey), new KeyPair(Utilities.HexToBinary(insecurePk.Value), Utilities.HexToBinary(insecureSk.Value)));
                    var freeRedemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(openMessage);
                    var coin = GetCoinAsync(freeRedemptionKey.Stamp, new CancellationToken()).GetAwaiter().GetResult();

                    if (coin == null)
                        return;

                    var swap = Swap(From(), 1, freeRedemptionKey.Key1, freeRedemptionKey.Key2, coin.Envelope);
                    var token1 = DeriveCoin(From(), swap.Item1.Version, swap.Item1.Envelope);
                    var status1 = VerifyCoin(swap.Item1, token1);
                    var token2 = DeriveCoin(From(), swap.Item2.Version, swap.Item2.Envelope);
                    var status2 = VerifyCoin(swap.Item2, token2);

                    if (status2 == 1)
                        walletService.AddEnvelope(Identifier(), From(), token2.Envelope).GetAwaiter();
                }
            }
        }

        public SecureString SecretKey()
        {
            return secretKey;
        }

        public ActorService SecretKey(SecureString sk)
        {
            secretKey = sk ?? throw new ArgumentNullException(nameof(sk));
            return this;
        }

        public void SendPayment(bool answer)
        {
            SetSecretKey().GetAwaiter();

            var token = DeriveCoin(From(), 0, DeriveEnvelope(From(), 1, Amount()));
            token = DeriveCoin(From(), 1, token.Envelope);

            var redemptionKey = HotRelease(token);
            var bobPk = DecodeAddress(To());
            var cipher = GetChiper(redemptionKey, bobPk);
            var sharedKey = GetSharedKey(bobPk);
            var notificationAddress = cryptography.GenericHashWithKey(Utilities.BinaryToHex(bobPk.ToArray()), sharedKey);
            var notification = new NotificationDto() { Address = Utilities.BinaryToHex(notificationAddress), Chiper = Convert.ToBase64String(cipher) };
            var delivered = false;

            if (answer)
                delivered = (bool)AddMessageAsync(notification, new CancellationToken()).GetAwaiter().GetResult();

            OnReceivedMessage(new ReceivedMessageEventArgs() { Message = delivered ? (object)true : notification.Chiper, ThroughSystem = answer });
        }

        public Tuple<CoinDto, CoinDto> Swap(SecureString password, int version, string key1, string key2, EnvelopeDto envelope)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (string.IsNullOrEmpty(key1))
            {
                throw new ArgumentException("Sub Key1 cannot be null or empty", nameof(key1));
            }

            if (string.IsNullOrEmpty(key2))
            {
                throw new ArgumentException("Sub Key2 cannot be null or empty", nameof(key2));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var stamp = cryptography.GenericHashNoKey(string.Format("{0}{1}", envelope.Amount.ToString(), envelope.Serial)).ToHex();
            var v1 = version + 1;
            var v2 = version + 2;
            var v3 = version + 3;
            var v4 = version + 4;

            var token1 = new CoinDto()
            {
                Keeper = DeriveKey(v2, stamp, DeriveKey(v3, stamp, DeriveKey(v3, stamp, password).ToSecureString()).ToSecureString()),
                Version = v1,
                Principle = key1,
                Stamp = stamp,
                Envelope = envelope,
                Hint = DeriveKey(v2, stamp, key2.ToSecureString())
            };

            var token2 = new CoinDto()
            {
                Keeper = DeriveKey(v3, stamp, DeriveKey(v4, stamp, DeriveKey(v4, stamp, password).ToSecureString()).ToSecureString()),
                Version = v2,
                Principle = key2,
                Stamp = stamp,
                Envelope = envelope,
                Hint = DeriveKey(v3, stamp, DeriveKey(v3, stamp, password).ToSecureString())
            };

            return Tuple.Create(token1, token2);
        }

        public CoinDto SwapPartialOne(SecureString password, RedemptionKeyDto redemptionKey)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (redemptionKey == null)
            {
                throw new ArgumentNullException(nameof(redemptionKey));
            }

            var coin = GetCoinAsync(redemptionKey.Stamp, new CancellationToken()).GetAwaiter().GetResult();

            if (coin != null)
            {
                var stamp = cryptography.GenericHashNoKey(string.Format("{0}{1}", coin.Envelope.Amount.ToString(), coin.Envelope.Serial)).ToHex();

                if (stamp.Equals(coin.Stamp))
                {
                    var v1 = coin.Version + 1;
                    var v2 = coin.Version + 2;
                    var v3 = coin.Version + 3;

                    coin.Keeper = DeriveKey(v2, stamp, DeriveKey(v3, stamp, DeriveKey(v3, stamp, password).ToSecureString()).ToSecureString());
                    coin.Version = v1;
                    coin.Principle = redemptionKey.Key1;
                    coin.Stamp = stamp;
                    coin.Envelope = coin.Envelope;
                    coin.Hint = redemptionKey.Key2;

                    return coin;
                }
            }

            return null;
        }

        public string To()
        {
            return _toAdress;
        }

        public ActorService To(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new Exception("To address is missing!");
            }

            _toAdress = address;

            return this;
        }

        public int VerifyCoin(CoinDto terminal, CoinDto current)
        {
            if (terminal == null)
            {
                throw new ArgumentNullException(nameof(terminal));
            }

            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            return terminal.Keeper.Equals(current.Keeper) && terminal.Hint.Equals(current.Hint)
               ? 1
               : terminal.Hint.Equals(current.Hint)
               ? 2
               : terminal.Keeper.Equals(current.Keeper)
               ? 3
               : 4;
        }

        private async Task SetSecretKey()
        {
            try
            {
                SecretKey(await walletService.GetStoreKey(Identifier(), From(), "SecretKey"));

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

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

        async Task<JObject> ClientPostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (!Equals(payload, default(T)))
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (baseAddress == null)
            {
                throw new ArgumentNullException(nameof(baseAddress));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is missing!", nameof(path));
            }

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    var content = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var buffer = Encoding.UTF8.GetBytes(content);

                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var result = Util.DeserializeJsonFromStream<JObject>(stream);
                            return Task.FromResult(result).Result;
                        }

                        var contentResult = await Util.StreamToStringAsync(stream);
                        throw new ApiException
                        {
                            StatusCode = (int)response.StatusCode,
                            Content = contentResult
                        };
                    }
                }
            }
        }

        async Task<T> ClientGetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
            {
                throw new ArgumentNullException(nameof(baseAddress));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is missing!", nameof(path));
            }

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                        return Util.DeserializeJsonFromStream<T>(stream);

                    var content = await Util.StreamToStringAsync(stream);
                    throw new ApiException
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = content
                    };
                }
            }
        }
    }
}