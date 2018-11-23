using System;
using System.Text;
using Newtonsoft.Json;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Actor
{
    public class ActorService : IActorService
    {
        //redemption
        protected string _masterKey;
        protected string _toAdress;
        protected double? _amount;
        protected string _memo;

        ChronicleDto _ChronicleDto;

        public ICryptography _Cryptography { get; }

        public ActorService(ICryptography cryptography)
        {
            _Cryptography = cryptography;
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

        public string DeriveKey(int n, string proof, string masterKey, int bytes = 32)
        {
            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty!", nameof(proof));
            }

            return _Cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", n, proof, masterKey), bytes).ToHex();
        }

        public ChronicleDto DeriveToken(string masterKey, int n, ProofTokenDto proofTokenDto)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new ArgumentException("Master key cannot be null or empty!", nameof(masterKey));
            }

            if (proofTokenDto == null)
            {
                throw new ArgumentNullException(nameof(proofTokenDto));
            }

            var proof = _Cryptography.GenericHashNoKey(string.Format("{0}{1}", proofTokenDto.Amount.ToString(), proofTokenDto.Serial)).ToHex();             var n0 = +n;             var n1 = +n + 1;             var n2 = +n + 2;              var chronicle = new ChronicleDto()             {                 Keeper = DeriveKey(n1, proof, DeriveKey(n2, proof, DeriveKey(n2, proof, masterKey))),                 N = n0,                 Principal = DeriveKey(n0, proof, masterKey),                 Proof = proof,                 ProofToken = proofTokenDto,                 Spark = DeriveKey(n1, proof, DeriveKey(n1, proof, masterKey))             };              return chronicle;
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

            var subKey1 = DeriveKey(chronicleDto.N + 1, chronicleDto.Proof, From());             var subKey2 = DeriveKey(chronicleDto.N + 2, chronicleDto.Proof, From());
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

            var publicKey = Encoding.UTF8.GetBytes(pkSkDto.PublicKey);             var privateKey = Encoding.UTF8.GetBytes(pkSkDto.SecretKey);             var cypher = Encoding.UTF8.GetBytes(cipher);             var message = _Cryptography.OpenBoxSeal(cypher, new Sodium.KeyPair(publicKey, privateKey));              return message;
        }

        void PaymentAddress(string key, int n, string proof) {
            
        }

        public void ReceivePayment(string redemptionKey)
        {
            if (string.IsNullOrEmpty(redemptionKey))
            {
                throw new ArgumentException("Redemption Key cannot be null or empty!", nameof(redemptionKey));
            }

            From("Nine inch nails...");

            var freeRedemptionKey = JsonConvert.DeserializeObject<RedemptionKeyDto>(redemptionKey);

            var swap = Swap(From(), 1, freeRedemptionKey.Key1, freeRedemptionKey.Key2, _ChronicleDto.ProofToken);              var token1 = DeriveToken(From(), swap.Item1.N, swap.Item1.ProofToken);             var status1 = VerifyToken(swap.Item1, token1);              var token2 = DeriveToken(From(), swap.Item2.N, swap.Item2.ProofToken);             var status2 = VerifyToken(swap.Item2, token2);
        }

        public void SendPayment()
        {
            _ChronicleDto = DeriveToken(From(), 0, new ProofTokenDto() { Amount = Amount().Value, Serial = _Cryptography.RandomKey().ToHex() });             _ChronicleDto = DeriveToken(From(), 1, _ChronicleDto.ProofToken);              var redemptionKey = HotRelease(_ChronicleDto);             // var base58 = Base58.Bitcoin.Decode(Util.Pop(To(), "_"));             // var cipher = _Cryptography.BoxSeal(redemptionKey, Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(base58).Substring(150)));


            ReceivePayment(redemptionKey);
        }

        public Tuple<ChronicleDto, ChronicleDto> Swap(string masterKey, int n, string key1, string key2, ProofTokenDto proofTokenDto)
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

            var proof = _Cryptography.GenericHashNoKey(string.Format("{0}{1}", proofTokenDto.Amount.ToString(), proofTokenDto.Serial)).ToHex();             var n1 = n + 1;             var n2 = n + 2;             var n3 = n + 3;             var n4 = n + 4;

            var chronicle1 = new ChronicleDto()             {                 Keeper = DeriveKey(n2, proof, DeriveKey(n3, proof, DeriveKey(n3, proof, masterKey))),                 N = n1,                 Principal = key1,                 Proof = proof,                 ProofToken = proofTokenDto,                 Spark = DeriveKey(n2, proof, key2)             };              var chronicle2 = new ChronicleDto()             {                 Keeper = DeriveKey(n3, proof, DeriveKey(n4, proof, DeriveKey(n4, proof, masterKey))),                 N = n2,                 Principal = key2,                 Proof = proof,                 ProofToken = proofTokenDto,                 Spark = DeriveKey(n3, proof, DeriveKey(n3, proof, masterKey))             };
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
    }
}