using System;
using System.Collections.Generic;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class WalletService : IWalletService
    {
        readonly ICryptography _Cryptography;

        public ICollection<PkSkDto> Store { get; set; }
        public string Id { get; set; }

        public WalletService(ICryptography cryptography)
        {
            _Cryptography = cryptography;
        }

        public PkSkDto CreatePkSk()
        {
            var kp = _Cryptography.KeyPair();

            return new PkSkDto() { PublicKey = kp.PublicKey.ToHex(), SecretKey = kp.SecretKey.ToHex() };
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