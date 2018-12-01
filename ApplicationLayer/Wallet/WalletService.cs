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
        public ICryptography _cryptography { get; }
        public string _id { get; set; }
        public ICollection<PkSkDto> _store { get; set; }

        public WalletService(ICryptography cryptography)
        {
            _cryptography = cryptography;
        }

        public PkSkDto CreatePkSk()
        {
            var kp = _cryptography.KeyPair();

            return new PkSkDto()
            {
                PublicKey = kp.PublicKey.ToHex(),
                SecretKey = kp.SecretKey.ToHex()
            };
        }

        public string MasterKey()
        {
            return _cryptography.RandomKey().ToHex();
        }

        public string NewID(int bytes = 32)
        {
            return string.Format("id_{0}", _cryptography.RandomBytes(bytes).ToHex());
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