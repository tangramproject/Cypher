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

        protected string _masterKey;
        protected string _toAdress;
        protected double? _amount;
        protected string _memo;

        ChronicleDto _ChronicleDto;
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

        public double? Amount()
        {
            return _amount;
        }

        public ActorService Amount(double? value)
        {
            if (value == null)             {                 throw new Exception("Value can not be null!");             }             if (Math.Abs(value.GetValueOrDefault()) <= 0)             {                 throw new Exception("Value can not be zero!");             }              _amount = value;

            return this;
        }

        public string DeriveKey(int version, string proof, string masterKey, int bytes = 32)
        {
            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty!", nameof(proof));
            }

            return _cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", version, proof, masterKey), bytes).ToHex();
        }

        public ChronicleDto DeriveToken(string masterKey, int version, ProofTokenDto proofTokenDto)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new ArgumentException("Master key cannot be null or empty!", nameof(masterKey));
            }

            if (proofTokenDto == null)
            {
                throw new ArgumentNullException(nameof(proofTokenDto));
            }

            var proof = _cryptography.GenericHashNoKey(string.Format("{0}{1}", proofTokenDto.Amount.ToString(), proofTokenDto.Serial)).ToHex();             var v0 = +version;             var v1 = +version + 1;             var v2 = +version + 2;              var chronicle = new ChronicleDto()             {                 Keeper = DeriveKey(v1, proof, DeriveKey(v2, proof, DeriveKey(v2, proof, masterKey))),                 Version = v0,                 Principal = DeriveKey(v0, proof, masterKey),                 Proof = proof,                 ProofToken = proofTokenDto,                 Spark = DeriveKey(v1, proof, DeriveKey(v1, proof, masterKey))             };              return chronicle;
        }

        public string From()
        {
            return _masterKey;
        }

        public ActorService From(string masterKey)
        {
            if (string.IsNullOrEmpty(masterKey))             {                 throw new Exception("Master Key is missing!");             }              _masterKey = masterKey;              return this;
        }

        public string HotRelease(ChronicleDto chronicleDto)
        {
            if (chronicleDto == null)
            {
                throw new ArgumentNullException(nameof(chronicleDto));
            }

            var subKey1 = DeriveKey(chronicleDto.Version + 1, chronicleDto.Proof, From());             var subKey2 = DeriveKey(chronicleDto.Version + 2, chronicleDto.Proof, From());
            var redemption = new RedemptionKeyDto() { Key1 = subKey1, Key2 = subKey2, Memo = Memo(), Proof = chronicleDto.Proof };

            return JsonConvert.SerializeObject(redemption);
        }

        public string Memo()
        {
            return _memo;
        }

        public ActorService Memo(string text)
        {
            if (string.IsNullOrEmpty(text))             {                 _memo = String.Empty;             }              if (text.Length > 64)             {                 throw new Exception("Memo field cannot be more than 64 characters long!");             }              _memo = text;              return this;
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

            var publicKey = Encoding.UTF8.GetBytes(pkSkDto.PublicKey);             var privateKey = Encoding.UTF8.GetBytes(pkSkDto.SecretKey);             var cypher = Encoding.UTF8.GetBytes(cipher);             var message = _cryptography.OpenBoxSeal(cypher, new Sodium.KeyPair(publicKey, privateKey));              return message;
        }

        void PaymentAddress(string key, int n, string proof)
        {

        }

        public void ReceivePayment(string redemptionKey)
        {
            if (string.IsNullOrEmpty(redemptionKey))
            {
                throw new ArgumentException("Redemption Key cannot be null or empty!", nameof(redemptionKey));
            }

            From("Nine inch nails...");

            var freeRedemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(redemptionKey);

            var swap = Swap(From(), 1, freeRedemptionKey.Key1, freeRedemptionKey.Key2, _ChronicleDto.ProofToken);              var token1 = DeriveToken(From(), swap.Item1.Version, swap.Item1.ProofToken);             var status1 = VerifyToken(swap.Item1, token1);              var token2 = DeriveToken(From(), swap.Item2.Version, swap.Item2.ProofToken);             var status2 = VerifyToken(swap.Item2, token2);
        }

        public void SendPayment()
        {
            _ChronicleDto = DeriveToken(From(), 0, new ProofTokenDto() { Amount = Amount().Value, Serial = _cryptography.RandomKey().ToHex() });             _ChronicleDto = DeriveToken(From(), 1, _ChronicleDto.ProofToken);              var redemptionKey = HotRelease(_ChronicleDto);             // var base58 = Base58.Bitcoin.Decode(Util.Pop(To(), "_"));             // var cipher = _Cryptography.BoxSeal(redemptionKey, Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(base58).Substring(150)));


            ReceivePayment(redemptionKey);
        }

        public Tuple<ChronicleDto, ChronicleDto> Swap(string masterKey, int version, string key1, string key2, ProofTokenDto proofTokenDto)
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

            if (proofTokenDto == null)
            {
                throw new ArgumentNullException(nameof(proofTokenDto));
            }

            var proof = _cryptography.GenericHashNoKey(string.Format("{0}{1}", proofTokenDto.Amount.ToString(), proofTokenDto.Serial)).ToHex();             var v1 = version + 1;             var v2 = version + 2;             var v3 = version + 3;             var v4 = version + 4;

            var chronicle1 = new ChronicleDto()             {                 Keeper = DeriveKey(v2, proof, DeriveKey(v3, proof, DeriveKey(v3, proof, masterKey))),                 Version = v1,                 Principal = key1,                 Proof = proof,                 ProofToken = proofTokenDto,                 Spark = DeriveKey(v2, proof, key2)             };              var chronicle2 = new ChronicleDto()             {                 Keeper = DeriveKey(v3, proof, DeriveKey(v4, proof, DeriveKey(v4, proof, masterKey))),                 Version = v2,                 Principal = key2,                 Proof = proof,                 ProofToken = proofTokenDto,                 Spark = DeriveKey(v3, proof, DeriveKey(v3, proof, masterKey))             };
             return Tuple.Create(chronicle1, chronicle2);
        }

        public string To()
        {
            return _toAdress;
        }

        public ActorService To(string address)
        {
            if (string.IsNullOrEmpty(address))             {                 throw new Exception("To address is missing!");             }              _toAdress = address;              return this;
        }

        public int VerifyToken(ChronicleDto terminal, ChronicleDto current)
        {
            if (terminal == null)
            {
                throw new ArgumentNullException(nameof(terminal));
            }

            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            return terminal.Keeper.Equals(current.Keeper) && terminal.Spark.Equals(current.Spark)
               ? 1
               : terminal.Spark.Equals(current.Spark)
               ? 2
               : terminal.Keeper.Equals(current.Keeper)
               ? 3
               : 4;
        }

        public async Task<ChronicleDto> FetchToken(string stamp, CancellationToken cancellationToken)
        {
            var url = String.Format("{0}{1}", _nodeSection.GetValue<string>(TOKEN), stamp);

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
                        return Util.DeserializeJsonFromStream<ChronicleDto>(stream);

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