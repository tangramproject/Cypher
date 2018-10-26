using System;
using System.Diagnostics.Contracts;
using System.Text;
using SimpleBase;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Actor
{

    public class ActorService : IActorService
    {
        readonly ICryptography _Cryptography;

        protected string _MasterKey;
        protected string _ToAdress;
        protected double? _Amount;
        protected string _Memo;

        ChronicleDto _ChronicleDto;

        public ActorService(ICryptography cryptography)
        {
            _Cryptography = cryptography;
        }

        public string From() { return _MasterKey; }
        public IActorService From(string masterKey)
        {
            if (string.IsNullOrEmpty(masterKey))
            {
                throw new Exception("Master Key is missing!");
            }

            _MasterKey = masterKey;

            return this;
        }

        public string To() { return _ToAdress; }
        public IActorService To(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new Exception("To address is missing!");
            }

            _ToAdress = address;

            return this;
        }

        public double? Amount() { return _Amount; }
        public IActorService Amount(double? value)
        {
            if (value == null)
            {
                throw new Exception("Value can not be null!");
            }
            if (Math.Abs(value.GetValueOrDefault()) < 0)
            {
                throw new Exception("Value can not be zero!");
            }

            _Amount = value;

            return this;
        }

        public string Memo() { return _Memo; }
        public IActorService Memo(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _Memo = String.Empty;
            }

            if (_Memo.Length > 64)
            {
                throw new Exception("Memo field cannot be more than 64 characters long!");
            }

            _Memo = text;

            return this;
        }

        public void SendPayment()
        {
            _ChronicleDto = DeriveToken(From(), 0, new ProofTokenDto() { Amount = Amount().Value, Serial = _Cryptography.RandomKey().ToHex() });
            _ChronicleDto = DeriveToken(From(), 1, _ChronicleDto.ProofToken);

            var commitmentKey = HotRelease(_ChronicleDto);
            var base58 = Base58.Bitcoin.Decode(Util.Pop(To(), "_"));
            var cipher = _Cryptography.BoxSeal(string.Format("{0}${1}", Memo(), commitmentKey), Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(base58).Substring(150)));

        }

        public void ReceivePayment(string commitmentKey)
        {
            Contract.Requires<ArgumentNullException>(string.IsNullOrEmpty(commitmentKey) == false);

            var swap = Swap(From(), 1, Util.FreeCommitmentKey(commitmentKey).Key1, Util.FreeCommitmentKey(commitmentKey).Key2, _ChronicleDto.ProofToken);

            var chronicle1 = new ChronicleDto()
            {
                Keeper = swap.Item1.Keeper,
                N = swap.Item1.N,
                Principal = swap.Item1.Principal,
                Proof = swap.Item1.Proof,
                ProofToken = swap.Item1.ProofToken,
                Spark = swap.Item1.Spark
            };
            var token1 = DeriveToken(From(), chronicle1.N, chronicle1.ProofToken);
            var status1 = VerifyToken(chronicle1, token1);

            var chronicle2 = new ChronicleDto()
            {
                Keeper = swap.Item2.Keeper,
                N = swap.Item2.N,
                Principal = swap.Item2.Principal,
                Proof = swap.Item2.Proof,
                ProofToken = swap.Item2.ProofToken,
                Spark = swap.Item2.Spark
            };
            var token2 = DeriveToken(From(), chronicle2.N, chronicle2.ProofToken);
            var status2 = VerifyToken(chronicle2, token2);
        }

        ChronicleDto DeriveToken(string masterKey, int n, ProofTokenDto proofTokenDto)
        {
            Contract.Requires<ArgumentNullException>(proofTokenDto != null);
            Contract.Requires<ArgumentNullException>(masterKey != null);

            var proof = _Cryptography.GenericHash(string.Format("{0}{1}", proofTokenDto.Amount.ToString(), proofTokenDto.Serial)).ToHex();
            var n0 = +n;
            var n1 = +n + 1;
            var n2 = +n + 2;

            var chronicle = new ChronicleDto()
            {
                Keeper = DeriveKey(n1, proof, DeriveKey(n2, proof, DeriveKey(n2, proof, masterKey))),
                N = n0,
                Principal = DeriveKey(n0, proof, masterKey),
                Proof = proof,
                ProofToken = proofTokenDto,
                Spark = DeriveKey(n1, proof, DeriveKey(n1, proof, masterKey))
            };

            return chronicle;
        }

        int VerifyToken(ChronicleDto terminal, ChronicleDto current)
        {
            return terminal.Keeper.Equals(current.Keeper) && terminal.Spark.Equals(current.Spark)
                           ? 1
                           : terminal.Spark.Equals(current.Spark)
                           ? 2
                           : terminal.Keeper.Equals(current.Keeper)
                           ? 3
                           : 4;
        }

        Tuple<ChronicleDto, ChronicleDto> Swap(string masterKey, int n, string key1, string key2, ProofTokenDto proofTokenDto)
        {
            var proof = _Cryptography.GenericHash(string.Format("{0}{1}", proofTokenDto.Amount.ToString(), proofTokenDto.Serial)).ToHex();
            var n1 = n + 1;
            var n2 = n + 2;
            var n3 = n + 3;
            var n4 = n + 4;

            var chronicle1 = new ChronicleDto()
            {
                Keeper = DeriveKey(n2, proof, DeriveKey(n3, proof, DeriveKey(n3, proof, masterKey))),
                N = n1,
                Principal = key1,
                Proof = proof,
                ProofToken = proofTokenDto,
                Spark = DeriveKey(n2, proof, key2)
            };

            var chronicle2 = new ChronicleDto()
            {
                Keeper = DeriveKey(n3, proof, DeriveKey(n4, proof, DeriveKey(n4, proof, masterKey))),
                N = n2,
                Principal = key2,
                Proof = proof,
                ProofToken = proofTokenDto,
                Spark = DeriveKey(n3, proof, DeriveKey(n3, proof, masterKey))
            };

            return Tuple.Create(chronicle1, chronicle2);
        }

        string HotRelease(ChronicleDto chronicleDto)
        {
            var subKey1 = DeriveKey(chronicleDto.N + 1, chronicleDto.Proof, From());
            var subKey2 = DeriveKey(chronicleDto.N + 2, chronicleDto.Proof, From());

            string[] boxed = { chronicleDto.Proof, subKey1, subKey2 };

            return Base58.Bitcoin.Encode(Encoding.UTF8.GetBytes(string.Join("", boxed)));
        }

        string OpenBoxSeal(string cipher, PkSkDto pkSkDto)
        {
            var publicKey = Encoding.UTF8.GetBytes(pkSkDto.PublicKey);
            var privateKey = Encoding.UTF8.GetBytes(pkSkDto.SecretKey);
            var cypher = Encoding.UTF8.GetBytes(cipher);
            var message = _Cryptography.OpenBoxSeal(cypher, new Sodium.KeyPair(publicKey, privateKey));

            return message;
        }

        string DeriveKey(int n, string proof, string masterKey)
        {
            return _Cryptography.GenericHash(string.Format("{0} {1} {2}", n, proof, masterKey)).ToHex();
        }
    }
}