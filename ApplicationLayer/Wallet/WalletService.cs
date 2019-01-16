using System;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using MurrayGrant.ReadablePassphrase;
using Newtonsoft.Json.Linq;
using SimpleBase;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class WalletService : IWalletService
    {
        readonly ICryptography cryptography;
        readonly IVaultService vaultService;

        public SecureString Id { get; set; }

        public WalletService(IVaultService vaultService, ICryptography cryptography)
        {
            this.vaultService = vaultService;
            this.cryptography = cryptography;
        }


        /// <summary>
        /// Gets the balance.
        /// </summary>
        /// <returns>The balance.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        public async Task<double> GetBalance(SecureString identifier, SecureString password)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            var total = 0.0;

            using (var insecureIdentifier = identifier.Insecure())
            {
                using (var insecurePassword = password.Insecure())
                {
                    var data = await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");

                    // TODO: Simple balance check. Clients will be challenged with ZKP.
                    if (data.Data.TryGetValue("envelopes", out object envelopes))
                    {
                        var list = ((JArray)envelopes).ToObject<List<EnvelopeDto>>();

                        for (int i = 0, listCount = list.Count; i < listCount; i++)
                        {
                            var item = list[i];
                            total += item.Amount;
                        }
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Creates the pk sk.
        /// </summary>
        /// <returns>The pk sk.</returns>
        public PkSkDto CreatePkSk()
        {
            var kp = cryptography.KeyPair();

            return new PkSkDto()
            {
                PublicKey = kp.PublicKey.ToHex(),
                SecretKey = kp.SecretKey.ToHex(),
                Address = Base58.Bitcoin.Encode(kp.PublicKey)
            };
        }

        /// <summary>
        /// News the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="bytes">Bytes.</param>
        public SecureString NewID(int bytes = 32)
        {
            var secureString = new SecureString();
            foreach (var c in string.Format("id_{0}", cryptography.RandomBytes(bytes).ToHex())) secureString.AppendChar(c);
            return secureString;
        }

        /// <summary>
        /// Passphrase this instance.
        /// </summary>
        /// <returns>The passphrase.</returns>
        public SecureString Passphrase()
        {
            var defaultDict = MurrayGrant.ReadablePassphrase.Dictionaries.Default.Load();
            var easyCreatedGenerator = Generator.Create();
            return easyCreatedGenerator.GenerateAsSecure(PhraseStrength.RandomForever);
        }

        /// <summary>
        /// Adds the envelope.
        /// </summary>
        /// <returns>The envelope.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="envelope">Envelope.</param>
        public async Task AddEnvelope(SecureString identifier, SecureString password, EnvelopeDto envelope)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            using (var insecureIdentifier = identifier.Insecure())
            {
                using (var insecurePassword = password.Insecure())
                {
                    var found = false;
                    var data = await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");

                    if (data.Data.TryGetValue("envelopes", out object envelopes))
                    {
                        foreach (JObject item in ((JArray)envelopes).Children().ToList())
                        {
                            var serial = item.GetValue("Serial");
                            found = serial.Value<string>().Equals(envelope.Serial);
                        }
                        if (!found)
                            ((JArray)envelopes).Add(JObject.FromObject(envelope));
                    }
                    else
                        data.Data.Add("envelopes", new List<EnvelopeDto> { envelope });

                    await vaultService.SaveDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet", data.Data);
                }
            }
        }

        /// <summary>
        /// Gets the store key.
        /// </summary>
        /// <returns>The store key.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="storeKey">Store key.</param>
        public async Task<SecureString> GetStoreKey(SecureString identifier, SecureString password, string storeKey)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(storeKey))
                throw new ArgumentException("Store key is missing!", nameof(storeKey));

            using (var insecureIdentifier = identifier.Insecure())
            {
                using (var insecurePassword = password.Insecure())
                {
                    var data = await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");
                    var storeKeys = JObject.FromObject(data.Data["storeKeys"]);
                    var key = storeKeys.GetValue(storeKey).Value<string>();
                    var secureString = new SecureString();

                    foreach (var c in key) secureString.AppendChar(Convert.ToChar(c));

                    return secureString;
                }
            }
        }
    }
}