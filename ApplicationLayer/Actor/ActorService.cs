using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        const string NODEAPI = "nodeAPI";
        const string ENDPOINT = "endpoint";
        const string TOKEN = "token";
        const string ADDTOKEN = "add";

        protected string _masterKey;
        protected string _toAdress;
        protected double? _amount;
        protected string _memo;
        protected string _secret;

        readonly IConfigurationSection _nodeSection;
        readonly ILogger _logger;

        public ICryptography _cryptography { get; }
        public IOnionService _onionService { get; }

        public ActorService(ICryptography cryptography, IOnionService onionService, IConfiguration configuration, ILogger logger)
        {
            _cryptography = cryptography;
            _onionService = onionService;
            _nodeSection = configuration.GetSection(NODEAPI);
            _logger = logger;
        }

        public async Task<JObject> AddToken(TokenDto token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var url = _nodeSection.GetValue<string>(ADDTOKEN);

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = new Uri(_nodeSection.GetValue<string>(ENDPOINT));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    var content = JsonConvert.SerializeObject(token, Formatting.None);
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

        public string PartialRelease(TokenDto token)
        {
            var subKey1 = DeriveKey(token.Version + 1, token.Stamp, From());
            var subKey2 = DeriveKey(token.Version + 2, token.Stamp, From());
            var mix = DeriveKey(token.Version + 2, token.Stamp, subKey2);
            var redemption = new RedemptionKeyDto() { Key1 = subKey1, Key2 = mix, Memo = Memo(), Proof = token.Stamp };

            return JsonConvert.SerializeObject(redemption);
        }

        public string DeriveKey(int version, string proof, string masterKey, int bytes = 32)
        {
            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty!", nameof(proof));
            }

            return _cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", version, proof, masterKey), bytes).ToHex();
        }

        public TokenDto DeriveToken(string masterKey, int version, EnvelopeDto envelope)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new ArgumentException("Master key cannot be null or empty!", nameof(masterKey));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var proof = _cryptography.GenericHashNoKey(string.Format("{0}{1}", envelope.Amount.ToString(), envelope.Serial)).ToHex();
            var v0 = +version;
            var v1 = +version + 1;
            var v2 = +version + 2;

            var chronicle = new TokenDto()
            {
                Keeper = DeriveKey(v1, proof, DeriveKey(v2, proof, DeriveKey(v2, proof, masterKey))),
                Version = v0,
                Principle = DeriveKey(v0, proof, masterKey),
                Stamp = proof,
                Envelope = envelope,
                Hint = DeriveKey(v1, proof, DeriveKey(v1, proof, masterKey))
            };

            return chronicle;
        }

        public async Task<TokenDto> FetchToken(string stamp, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(stamp))
            {
                throw new ArgumentException("Stamp is missing!", nameof(stamp));
            }

            var url = string.Format("{0}{1}", _nodeSection.GetValue<string>(TOKEN), stamp);

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = new Uri(_nodeSection.GetValue<string>(ENDPOINT));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                        return Util.DeserializeJsonFromStream<TokenDto>(stream);

                    var content = await Util.StreamToStringAsync(stream);
                    throw new ApiException
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = content
                    };
                }
            }
        }

        public string From()
        {
            return _masterKey;
        }

        public ActorService From(string masterKey)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new Exception("Master Key is missing!");
            }

            _masterKey = masterKey;

            return this;
        }

        public string HotRelease(TokenDto token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(TokenDto));
            }

            var subKey1 = DeriveKey(token.Version + 1, token.Stamp, From());
            var subKey2 = DeriveKey(token.Version + 2, token.Stamp, From());
            var redemption = new RedemptionKeyDto() { Key1 = subKey1, Key2 = subKey2, Memo = Memo(), Proof = token.Stamp };

            return JsonConvert.SerializeObject(redemption);
        }

        public string Memo()
        {
            return _memo;
        }

        public ActorService Memo(string text)
        {
            if (string.IsNullOrEmpty(text))             {                 _memo = string.Empty;             }              if (text.Length > 64)             {                 throw new Exception("Memo field cannot be more than 64 characters long!");             }              _memo = text;              return this;
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

            var publicKey = Encoding.UTF8.GetBytes(pkSkDto.PublicKey);
            var privateKey = Encoding.UTF8.GetBytes(pkSkDto.SecretKey);
            var cypher = Encoding.UTF8.GetBytes(cipher);
            var message = _cryptography.OpenBoxSeal(cypher, new Sodium.KeyPair(publicKey, privateKey));

            return message;
        }

        public void ReceivePayment(string redemptionKey)
        {
            if (string.IsNullOrEmpty(redemptionKey))
            {
                throw new ArgumentException("Redemption Key cannot be null or empty!", nameof(redemptionKey));
            }

            From("Nine inch nails...");

            var freeRedemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(redemptionKey);

            // var swap = Swap(From(), 1, freeRedemptionKey.Key1, freeRedemptionKey.Key2, _tokenDto.Envelope);
            var swap = Swap(From(), 1, freeRedemptionKey.Key1, freeRedemptionKey.Key2, null);              var token1 = DeriveToken(From(), swap.Item1.Version, swap.Item1.Envelope);             var status1 = VerifyToken(swap.Item1, token1);              var token2 = DeriveToken(From(), swap.Item2.Version, swap.Item2.Envelope);             var status2 = VerifyToken(swap.Item2, token2);
        }

        public string Secret()
        {
            return _secret;
        }

        public ActorService Secret(string sk)
        {
            if (string.IsNullOrEmpty(sk))
            {
                _secret = string.Empty;
            }

            _secret = sk;

            return this;
        }

        public void SendPayment()
        {
            var token = DeriveToken(From(), 0, new EnvelopeDto() { Amount = Amount().Value, Serial = _cryptography.RandomBytes(16).ToHex() });             token = DeriveToken(From(), 1, token.Envelope);              var redemptionKey = HotRelease(token);             var bobPk = Base58.Bitcoin.Decode(To());             var cipher = _cryptography.BoxSeal(redemptionKey, bobPk.ToArray());
            var sharedKey = _cryptography.ScalarMult(Utilities.HexToBinary(Secret()), bobPk.ToArray());
            var notificationAddress = _cryptography.GenericHashWithKey(Utilities.BinaryToHex(bobPk.ToArray()), sharedKey);
        }

        public Tuple<TokenDto, TokenDto> Swap(string masterKey, int version, string key1, string key2, EnvelopeDto envelope)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new ArgumentException("Master Key cannot be null or empty!", nameof(masterKey));
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

            var proof = _cryptography.GenericHashNoKey(string.Format("{0}{1}", envelope.Amount.ToString(), envelope.Serial)).ToHex();
            var v1 = version + 1;
            var v2 = version + 2;
            var v3 = version + 3;
            var v4 = version + 4;

            var token1 = new TokenDto()
            {
                Keeper = DeriveKey(v2, proof, DeriveKey(v3, proof, DeriveKey(v3, proof, masterKey))),
                Version = v1,
                Principle = key1,
                Stamp = proof,
                Envelope = envelope,
                Hint = DeriveKey(v2, proof, key2)
            };

            var token2 = new TokenDto()
            {
                Keeper = DeriveKey(v3, proof, DeriveKey(v4, proof, DeriveKey(v4, proof, masterKey))),
                Version = v2,
                Principle = key2,
                Stamp = proof,
                Envelope = envelope,
                Hint = DeriveKey(v3, proof, DeriveKey(v3, proof, masterKey))
            };

            return Tuple.Create(token1, token2);
        }

        public TokenDto SwapPartialOne(string masterKey, RedemptionKeyDto redemptionKey)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new ArgumentException("Master Key cannot be null or empty!", nameof(masterKey));
            }

            if (redemptionKey == null)
            {
                throw new ArgumentNullException(nameof(redemptionKey));
            }

            var token = FetchToken(redemptionKey.Proof, new CancellationToken()).GetAwaiter().GetResult();

            if (token != null)
            {
                var proof = _cryptography.GenericHashNoKey(string.Format("{0}{1}", token.Envelope.Amount.ToString(), token.Envelope.Serial)).ToHex();

                if (proof.Equals(token.Stamp))
                {
                    var v1 = token.Version + 1;
                    var v2 = token.Version + 2;
                    var v3 = token.Version + 3;

                    token.Keeper = DeriveKey(v2, proof, DeriveKey(v3, proof, DeriveKey(v3, proof, masterKey)));
                    token.Version = v1;
                    token.Principle = redemptionKey.Key1;
                    token.Stamp = proof;
                    token.Envelope = token.Envelope;
                    token.Hint = redemptionKey.Key2;

                    return token;
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

        public int VerifyToken(TokenDto terminal, TokenDto current)
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
    }
}