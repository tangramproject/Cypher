using System;
using System.Collections.Generic;
using System.Linq;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class WalletService : IWalletService
    {
        public ICryptography _Cryptography { get; }
        public string Id { get; set; }
        public ICollection<PkSkDto> Store { get; set; }

        public WalletService(ICryptography cryptography)
        {
            _Cryptography = cryptography;
        }

        public PkSkDto CreatePkSk()
        {
            var kp = _Cryptography.KeyPair();

            return new PkSkDto()
            {
                Proof = _Cryptography.GenericHash(_Cryptography.RandomKey().ToHex(), 16).ToHex(),
                PublicKey = kp.PublicKey.ToHex(),
                SecretKey = kp.SecretKey.ToHex(),
                SplitKeys = Util.Split(_Cryptography.RandomKey().ToHex(), 16).ToArray()
            };
        }

        public string MasterKey()
        {
            return _Cryptography.RandomKey().ToHex();
        }

        public string NewID()
        {
            return String.Format("id_{0}", _Cryptography.RandomKey().ToHex());
        }

        public string Passphrase(int listOfWords)
        {
            throw new NotImplementedException();
        }
    }
}