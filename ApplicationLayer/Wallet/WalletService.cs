using System;
using System.Collections.Generic;
using System.Linq;
using MurrayGrant.ReadablePassphrase;
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
                PublicKey = kp.PublicKey.ToHex(),
                SecretKey = kp.SecretKey.ToHex()
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

        public string Passphrase()
        {
            var defaultDict = MurrayGrant.ReadablePassphrase.Dictionaries.Default.Load();
            var easyCreatedGenerator = Generator.Create();
            var secureString = easyCreatedGenerator.GenerateAsSecure(PhraseStrength.RandomForever);

            return Util.ToPlainString(secureString);
        }

    }
}