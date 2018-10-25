using System;
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
                throw new Exception("memo can not be null or empty!");
            }

            _Memo = text;

            return this;
        }

        public void SendPayment()
        {

        }

        public void ReceivePayment()
        {

        }

        ChronicleDto DeriveToken(string masterKey, int n, ProofTokenDto proofTokenDto)
        {
            return null;
        }

        ChronicleDto VerifyToken()
        {
            return null;
        }

        Tuple<ChronicleDto, ChronicleDto> Swap()
        {
            return null;
        }

        string HotRelease(ChronicleDto chronicleDto)
        {
            return null;
        }

        string UnpackSealbox() {
            return null;
        }
    }
}