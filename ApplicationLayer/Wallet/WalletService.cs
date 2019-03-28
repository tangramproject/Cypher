// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using MurrayGrant.ReadablePassphrase;
using Newtonsoft.Json.Linq;
using SimpleBase;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helper;
using TangramCypher.Helper.LibSodium;
using TangramCypher.ApplicationLayer.Coin;
using TangramCypher.ApplicationLayer.Actor;
using System.Text;
using TangramCypher.ApplicationLayer.Helper.ZeroKP;
using Microsoft.Extensions.Configuration;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly IVaultService vaultService;
        private readonly IConfigurationSection apiNetworkSection;
        private readonly string environment;
        public WalletService(IVaultService vaultService, IConfiguration configuration)
        {
            this.vaultService = vaultService;

            apiNetworkSection = configuration.GetSection(Constant.ApiNetwork);
            environment = apiNetworkSection.GetValue<string>(Constant.Environment);
        }

        /// <summary>
        /// Gets the available balance.
        /// </summary>
        /// <returns>The balance.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        public async Task<double> AvailableBalance(SecureString identifier, SecureString password)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            double total = 0.0d;
            var transactions = await Transactions(identifier, password);

            if (transactions != null)
            {
                double pocket = 0.0d;
                TransactionDto burnt = null;

                try
                {
                    pocket = transactions.Where(tx => tx.TransactionType == TransactionType.Receive).Skip(1).Sum(p => p.Amount);
                    burnt = transactions.Last(tx => tx.TransactionType == TransactionType.Send);
                }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                finally
                {
                    switch (burnt)
                    {
                        case null:
                            total = transactions.Where(tx => tx.TransactionType == TransactionType.Receive).Sum(p => p.Amount);
                            break;
                        default:
                            {
                                total = burnt.Amount + pocket;
                                break;
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
            var kp = Cryptography.KeyPair();

            return new PkSkDto()
            {
                PublicKey = kp.PublicKey.ToHex(),
                SecretKey = kp.SecretKey.ToHex(),
                Address = Encoding.UTF8.GetString(NetworkAddress(kp.PublicKey))
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
            foreach (var c in string.Format("id_{0}", Cryptography.RandomBytes(bytes).ToHex())) secureString.AppendChar(c);
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
        /// Hashs the password.
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="passphrase">Passphrase.</param>
        public byte[] HashPassword(SecureString passphrase) => Cryptography.ArgonHashPassword(passphrase);


        /// <summary>
        /// Adds the transaction.
        /// </summary>
        /// <returns>The transaction.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="transaction">Transaction.</param>
        public async Task AddTransaction(SecureString identifier, SecureString password, TransactionDto transaction)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using (var insecureIdentifier = identifier.Insecure())
            {
                var found = false;
                var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");

                if (data.Data.TryGetValue("transactions", out object txs))
                {
                    foreach (JObject item in ((JArray)txs).Children().ToList())
                    {
                        var hash = item.GetValue("Hash");
                        found = hash.Value<string>().Equals(transaction.Hash);
                    }
                    if (!found)
                        ((JArray)txs).Add(JObject.FromObject(transaction));
                }
                else
                    data.Data.Add("transactions", new List<TransactionDto> { transaction });

                await vaultService.SaveDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet", data.Data);
            }
        }

        /// <summary>
        /// Adds if not found or updates message tracking.
        /// </summary>
        /// <returns>The message tracking.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="messageTrack">Message track.</param>
        public async Task AddMessageTracking(SecureString identifier, SecureString password, MessageTrackDto messageTrack)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (messageTrack == null)
                throw new ArgumentNullException(nameof(messageTrack));

            using (var insecureIdentifier = identifier.Insecure())
            {
                var found = false;
                var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");

                if (data.Data.TryGetValue("messages", out object msgs))
                {
                    foreach (JObject item in ((JArray)msgs).Children().ToList())
                    {
                        var pk = item.GetValue("PublicKey");
                        found = pk.Value<string>().Equals(messageTrack.PublicKey);
                    }

                    if (!found)
                        ((JArray)msgs).Add(JObject.FromObject(messageTrack));
                    else
                        ((JArray)msgs).Replace(JObject.FromObject(messageTrack));
                }
                else
                    data.Data.Add("messages", new List<MessageTrackDto> { messageTrack });

                await vaultService.SaveDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet", data.Data);
            }
        }

        /// <summary>
        /// Gets the stored message track.
        /// </summary>
        /// <returns>The track.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="pk">Pk.</param>
        public async Task<MessageTrackDto> MessageTrack(SecureString identifier, SecureString password, string pk)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            MessageTrackDto messageTrack = null;

            using (var insecureIdentifier = identifier.Insecure())
            {
                var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");
                if (data.Data.TryGetValue("messages", out object msgs))
                {
                    messageTrack = ((JArray)msgs).ToObject<List<MessageTrackDto>>().FirstOrDefault(msg => msg.PublicKey.Equals(pk));
                }
            }

            return messageTrack;
        }

        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <returns>The transaction.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="hash">Hash.</param>
        public async Task<TransactionDto> Transaction(SecureString identifier, SecureString password, string hash)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("Hash is missing!", nameof(hash));

            var transactions = await Transactions(identifier, password);

            return transactions.FirstOrDefault(t => t.Hash.Equals(hash));
        }

        /// <summary>
        /// Gets the transaction amount.
        /// </summary>
        /// <returns>The transaction amount.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="stamp">Stamp.</param>
        public async Task<double> TransactionAmount(SecureString identifier, SecureString password, string stamp)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentException("Stamp is missing!", nameof(stamp));

            var total = 0.0D;
            var transactions = await Transactions(identifier, password);
            var transaction = transactions.Select(tx =>
            {
                if (tx.Stamp.Equals(stamp))
                    if (double.TryParse(tx.Amount.ToString(), out double t))
                        total = t;

                return total;
            });

            return transaction.FirstOrDefault();
        }

        /// <summary>
        /// Gets the envelope.
        /// </summary>
        /// <returns>The envelope.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        public async Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            List<TransactionDto> transactions = null;

            using (var insecureIdentifier = identifier.Insecure())
            {
                var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");
                if (data.Data.TryGetValue("transactions", out object txs))
                {
                    transactions = ((JArray)txs).ToObject<List<TransactionDto>>();
                }
            }

            return transactions;
        }

        /// <summary>
        /// Gets the transactions by stamp.
        /// </summary>
        /// <returns>The transactions.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="stamp">Stamp.</param>
        public async Task<List<TransactionDto>> Transactions(SecureString identifier, SecureString password, string stamp)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            var transactions = await Transactions(identifier, password);

            if (transactions != null)
                transactions = transactions.Where(tx => tx.Stamp == stamp).ToList();

            return transactions;
        }

        /// <summary>
        /// Gets the store key.
        /// </summary>
        /// <returns>The store key.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="storeKey">Store key.</param>
        public async Task<SecureString> StoreKey(SecureString identifier, SecureString password, string storeKey)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(storeKey))
                throw new ArgumentException("Store key is missing!", nameof(storeKey));

            using (var insecureIdentifier = identifier.Insecure())
            {
                var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");
                var storeKeys = JObject.FromObject(data.Data["storeKeys"]);
                var key = storeKeys.GetValue(storeKey).Value<string>();
                var secureString = new SecureString();

                foreach (var c in key) secureString.AppendChar(Convert.ToChar(c));

                return secureString;
            }
        }

        /// <summary>
        /// Makes the change.
        /// </summary>
        /// <returns>The change.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="amount">Amount.</param>
        public async Task<TransactionChange> MakeChange(SecureString identifier, SecureString password, double amount)
        {
            if (!double.TryParse(amount.ToString(), out double t))
                throw new InvalidCastException();

            var transactions = await Transactions(identifier, password);
            var coins = transactions.OrderBy(tx => tx.Version).ToArray();
            var transactionChange = CalculateChange(amount, ref t, transactions);

            return transactionChange;
        }

        /// <summary>
        /// Makes the change.
        /// </summary>
        /// <returns>The change.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="amount">Amount.</param>
        /// <param name="stamp">Stamp.</param>
        public async Task<TransactionChange> MakeChange(SecureString identifier, SecureString password, double amount, string stamp)
        {
            if (!double.TryParse(amount.ToString(), out double t))
                throw new InvalidCastException();

            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentException("message", nameof(stamp));

            var transactions = await Transactions(identifier, password, stamp);
            var transactionChange = CalculateChange(amount, ref t, transactions);

            return transactionChange;
        }

        /// <summary>
        /// Calculates the change.
        /// </summary>
        /// <returns>The change.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="t">T.</param>
        /// <param name="transactions">Transactions.</param>
        private static TransactionChange CalculateChange(double amount, ref double t, List<TransactionDto> transactions)
        {
            var coins = transactions.OrderBy(tx => tx.Version).ToArray();
            int count, i;
            var transactionChange = new TransactionChange();

            for (i = 0; i < coins.Length; i++)
            {
                count = (int)(t / Math.Abs(coins[i].Amount));
                if (count != 0)
                {
                    Console.WriteLine("Count of {0} change :{1}", coins[i].Amount, count);
                    for (int k = 0; k < count; k++) transactionChange.Transactions.Add(coins[i]);
                }

                t %= coins[i].Amount;
            }

            var sum = transactionChange.Transactions.Sum(s => Math.Abs(s.Amount));
            var remainder = amount - sum;
            var closest = coins.Select(x => Math.Abs(x.Amount)).Aggregate((x, y) => Math.Abs(x - remainder) < Math.Abs(y - remainder) ? x : y);

            transactionChange.AmountFor = remainder;
            transactionChange.Transaction = coins.FirstOrDefault(a => a.Amount.Equals(closest));

            return transactionChange;
        }

        /// <summary>
        /// Network address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="coin">Coin.</param>
        /// <param name="networkApi">Network API.</param>
        public byte[] NetworkAddress(CoinDto coin, NetworkApiMethod networkApi = null)
        {
            string env = string.Empty;
            byte[] address = new byte[33];

            env = networkApi == null ? environment : networkApi.ToString();
            address[0] = networkApi.ToString().Equals(env) ? (byte)0x1 : (byte)74;

            var hash = Cryptography.GenericHashWithKey(
                $"{coin.Envelope.Commitment}" +
                $" {coin.Envelope.Proof}" +
                $" {coin.Envelope.PublicKey}" +
                $" {coin.Envelope.Signature}" +
                $" {coin.Hash}" +
                $" {coin.Hint}" +
                $" {coin.Keeper}" +
                $" {coin.Principle}" +
                $" {coin.Stamp}" +
                $" {coin.Version}",
                Encoding.UTF8.GetBytes(coin.Principle));

            Array.Copy(hash, 0, address, 1, 32);

            return Encoding.UTF8.GetBytes(Base58.Bitcoin.Encode(address));
        }

        /// <summary>
        /// Network address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="pk">Pk.</param>
        /// <param name="networkApi">Network API.</param>
        public byte[] NetworkAddress(byte[] pk, NetworkApiMethod networkApi = null)
        {
            string env = string.Empty;
            byte[] address = new byte[33];

            env = networkApi == null ? environment : networkApi.ToString();
            address[0] = networkApi.ToString().Equals(env) ? (byte)0x1 : (byte)74;

            Array.Copy(pk, 0, address, 1, 32);

            return Encoding.UTF8.GetBytes(Base58.Bitcoin.Encode(address));
        }

        /// <summary>
        /// Returns provers password.
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="password">Password.</param>
        /// <param name="version">Version.</param>
        public string ProverPassword(SecureString password, int version)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            using (var insecurePassword = password.Insecure())
            {
                var hash = Cryptography.GenericHashNoKey(string.Format("{0} {1}", version, insecurePassword.Value));

                return Prover.GetHashStringNumber(hash).ToByteArray().ToHex();
            }
        }
    }
}