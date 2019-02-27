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

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class WalletService : IWalletService
    {
        private const string Transactions = "transactions";
        private const string Hash = "Hash";

        private readonly IVaultService vaultService;

        public WalletService(IVaultService vaultService)
        {
            this.vaultService = vaultService;
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

            var total = 0.0D;
            var transactions = await GetTransactions(identifier, password);

            if (transactions != null)
            {
                foreach (var transaction in transactions)
                {
                    if (double.TryParse(transaction.Amount.ToString(), out double t))
                        total = t;
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
        /// <param name="coin">Coin.</param>
        public async Task AddTransaction(SecureString identifier, SecureString password, TransactionDto transaction)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using (var insecureIdentifier = identifier.Insecure())
            using (var insecurePassword = password.Insecure())
            {
                var found = false;
                var data = await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");

                if (data.Data.TryGetValue(Transactions, out object txs))
                {
                    foreach (JObject item in ((JArray)txs).Children().ToList())
                    {
                        var hash = item.GetValue(Hash);
                        found = hash.Value<string>().Equals(transaction.Hash);
                    }
                    if (!found)
                        ((JArray)txs).Add(JObject.FromObject(transaction));
                }
                else
                    data.Data.Add(Transactions, new List<TransactionDto> { transaction });

                await vaultService.SaveDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet", data.Data);
            }
        }

        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <returns>The transaction.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="hash">Hash.</param>
        public async Task<TransactionDto> GetTransaction(SecureString identifier, SecureString password, string hash)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("Hash is missing!", nameof(hash));

            var transactions = await GetTransactions(identifier, password);

            return transactions.FirstOrDefault(t => t.Hash.Equals(hash));
        }

        /// <summary>
        /// Gets the transaction amount.
        /// </summary>
        /// <returns>The transaction amount.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="stamp">Stamp.</param>
        public async Task<double> GetTransactionAmount(SecureString identifier, SecureString password, string stamp)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentException("Stamp is missing!", nameof(stamp));

            var total = 0.0D;
            var transactions = await GetTransactions(identifier, password);
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
        public async Task<List<TransactionDto>> GetTransactions(SecureString identifier, SecureString password)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            List<TransactionDto> transactions = null;

            using (var insecureIdentifier = identifier.Insecure())
            using (var insecurePassword = password.Insecure())
            {
                var data = await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");
                if (data.Data.TryGetValue(Transactions, out object txs))
                {
                    transactions = ((JArray)txs).ToObject<List<TransactionDto>>();
                }
            }

            return transactions;
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

            var transactions = await GetTransactions(identifier, password);
            var coins = transactions.OrderByDescending(a => Math.Abs(a.Amount)).ToArray();
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


    }
}