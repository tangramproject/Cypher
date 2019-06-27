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
using MurrayGrant.ReadablePassphrase;
using SimpleBase;
using TangramCypher.Helper;
using TangramCypher.Helper.LibSodium;
using TangramCypher.ApplicationLayer.Actor;
using System.Text;
using TangramCypher.ApplicationLayer.Helper.ZeroKP;
using Microsoft.Extensions.Configuration;
using Dawn;
using Microsoft.Extensions.Logging;
using TangramCypher.Model;
using System.IO;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly IConfigurationSection apiNetworkSection;
        private readonly ILogger logger;
        private readonly string environment;

        public WalletService(IConfiguration configuration, ILogger logger)
        {
            apiNetworkSection = configuration.GetSection(Constant.ApiNetwork);
            environment = apiNetworkSection.GetValue<string>(Constant.Environment);

            this.logger = logger;
        }

        /// <summary>
        /// Gets the generic available balance.
        /// </summary>
        /// <returns>The balance.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        public TaskResult<ulong> AvailableBalance(SecureString identifier, SecureString password)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(password, nameof(password)).NotNull();

            ulong balance;

            try
            {
                using (var db = Util.LiteRepositoryFactory(password, identifier.ToUnSecureString()))
                {
                    var txns = db.Fetch<TransactionDto>();
                    if (txns?.Any() != true)
                    {
                        return TaskResult<ulong>.CreateSuccess(0);
                    }

                    balance = Balance(txns);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return TaskResult<ulong>.CreateFailure(ex);
            }

            return TaskResult<ulong>.CreateSuccess(balance);
        }

        public void AddKeySet(SecureString secret, string identifier)
        {
            try
            {
                using (var db = Util.LiteRepositoryFactory(secret, identifier))
                {
                    var keySet = CreateKeySet();
                    db.Insert(keySet);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Creates new secret/public address key.
        /// </summary>
        /// <returns>The pk sk.</returns>
        public KeySetDto CreateKeySet()
        {
            var kp = Cryptography.KeyPair();

            return new KeySetDto
            {
                PublicKey = kp.PublicKey.ToHex(),
                SecretKey = kp.SecretKey.ToHex(),
                Address = Encoding.UTF8.GetString(NetworkAddress(kp.PublicKey))
            };
        }

        /// <summary>
        /// Create new wallet.
        /// </summary>
        /// <returns>The wallet.</returns>
        public CredentialsDto CreateWallet()
        {
            var walletId = NewID(16);
            var passphrase = Passphrase();
            var keySet = CreateKeySet();

            walletId.MakeReadOnly();
            passphrase.MakeReadOnly();

            try
            {
                using (var db = Util.LiteRepositoryFactory(passphrase, walletId.ToUnSecureString()))
                {
                    db.Insert(keySet);
                }

                return new CredentialsDto { Identifier = walletId.ToUnSecureString(), Password = passphrase.ToUnSecureString() };
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw new Exception("Failed to create wallet.");
            }
            finally
            {
                walletId.Dispose();
                passphrase.Dispose();
            }
        }

        /// <summary>
        /// Creates a new identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="bytes">Bytes.</param>
        public SecureString NewID(int bytes = 32)
        {
            var secureString = new SecureString();
            foreach (var c in $"id_{Cryptography.RandomBytes(bytes).ToHex()}") secureString.AppendChar(c);
            return secureString;
        }

        /// <summary>
        /// Creates a new passphrase.
        /// </summary>
        /// <returns>The passphrase.</returns>
        public SecureString Passphrase()
        {
            _ = MurrayGrant.ReadablePassphrase.Dictionaries.Default.Load();
            var easyCreatedGenerator = Generator.Create();
            return easyCreatedGenerator.GenerateAsSecure(PhraseStrength.RandomForever);
        }

        /// <summary>
        /// Hashs the password.
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="passphrase">Passphrase.</param>
        public byte[] HashPassword(SecureString passphrase) => Cryptography.ArgonHashString(passphrase);

        /// <summary>
        /// Gets the total transaction amount.
        /// </summary>
        /// <returns>The transaction amount.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        /// <param name="stamp">Stamp.</param>
        public ulong TotalTransactionAmount(SecureString identifier, SecureString password, string stamp)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();

            ulong total;

            using (var db = Util.LiteRepositoryFactory(password, identifier.ToUnSecureString()))
            {
                var txns = db.Fetch<TransactionDto>();
                if (txns?.Any() != true)
                {
                    return 0;
                }

                var amounts = txns.Where(tx => tx.Stamp.Equals(stamp)).Select(a => a.Amount);
                total = Util.Sum(amounts);
            }

            return total;
        }

        /// <summary>
        /// Last transaction amount.
        /// </summary>
        /// <returns>The transaction amount.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="password">Password.</param>
        public TransactionDto LastTransaction(SecureString identifier, SecureString password, TransactionType transactionType)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(password, nameof(password)).NotNull();

            TransactionDto transaction;

            using (var db = Util.LiteRepositoryFactory(password, identifier.ToUnSecureString()))
            {
                var txns = db.Fetch<TransactionDto>();
                if (txns?.Any() != true)
                {
                    return null;
                }

                transaction = txns.Last(tx => tx.TransactionType.Equals(transactionType));
            }

            return transaction;
        }

        /// <summary>
        /// Sorts the change.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public TaskResult<PurchaseDto> SortChange(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            List<TransactionDto> txns;

            using (var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString()))
            {
                txns = db.Fetch<TransactionDto>();
                if (txns?.Any() != true)
                {
                    return null;
                }
            }

            PurchaseDto purchase = null;

            try
            {
                TransactionDto[] txsIn = txns.Where(tx => tx.TransactionType == TransactionType.Receive).OrderBy(tx => tx.Version).ToArray();
                TransactionDto[] target = new TransactionDto[txsIn.Length];

                Array.Copy(txsIn, target, txsIn.Length);

                for (int i = 0, targetLength = target.Length; i < targetLength; i++)
                {
                    (TransactionDto transaction, double amountFor) = CalculateChange(session.Amount, txsIn);
                    var balance = Balance(txns.Where(tx => tx.Stamp == transaction.Stamp).ToList());

                    if (balance >= amountFor)
                    {
                        purchase = new PurchaseDto
                        {
                            Balance = balance,
                            DateTime = DateTime.Now,
                            Input = session.Amount,
                            Output = balance - session.Amount,
                            Stamp = transaction.Stamp,
                            TransactionId = session.SessionId
                        };

                        purchase.Chain = txns.Where(tx => tx.Stamp.Equals(transaction.Stamp)).Select(tx => Guid.Parse(tx.TransactionId)).ToHashSet();
                        purchase.Version = txns.Last(tx => tx.Stamp.Equals(transaction.Stamp) && tx.TransactionId.Equals(purchase.Chain.Last().ToString())).Version;

                        if (purchase.Output.Equals(0))
                            purchase.Spent = true;

                        break;
                    }

                    var idx = Array.FindIndex(txsIn, t => t.Stamp.Equals(transaction.Stamp));
                    txsIn = txsIn.Where((source, index) => index != idx).ToArray();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return TaskResult<PurchaseDto>.CreateFailure(ex);
            }

            return TaskResult<PurchaseDto>.CreateSuccess(purchase);
        }

        /// <summary>
        /// Lists the wallets available.
        /// </summary>
        /// <returns>The identifier list.</returns>
        public IEnumerable<string> WalletList()
        {
            var wallets = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "wallets");
            string[] files = Directory.GetFiles(wallets, "*.db");

            if (files?.Any() != true)
            {
                return Enumerable.Empty<string>();
            }

            return files;
        }

        public IEnumerable<string> ListAddresses(SecureString secret, string identifier)
        {
            using (var db = Util.LiteRepositoryFactory(secret, identifier))
            {
                var keys = db.Fetch<KeySetDto>();
                if (keys?.Any() != true)
                {
                    return Enumerable.Empty<string>();
                }

                return keys.Select(k => k.Address);
            }
        }

        /// <summary>
        /// Calculate balance from transactions.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private ulong Balance(IEnumerable<TransactionDto> source)
        {
            var total = 0UL;

            if (source != null)
            {
                ulong? pocket = null;
                ulong? burnt = null;

                try
                {
                    pocket = Util.Sum(source, TransactionType.Receive);
                    burnt = Util.Sum(source, TransactionType.Send);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    throw ex;
                }
                finally
                {
                    switch (burnt)
                    {
                        case null:
                            total = pocket == null ? 0 : pocket.Value;
                            break;
                        default:
                            {
                                total = pocket.Value - burnt.Value;
                                break;
                            }
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Calculates the change.
        /// </summary>
        /// <returns>The change.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="transactions">Transactions.</param>
        private (TransactionDto, ulong) CalculateChange(ulong amount, TransactionDto[] transactions)
        {
            Guard.Argument(transactions, nameof(transactions)).NotNull();

            int count;
            var tempTxs = new List<TransactionDto>();

            for (var i = 0; i < transactions.Length; i++)
            {
                count = (int)(amount / transactions[i].Amount);
                if (count != 0)
                    for (int k = 0; k < count; k++) tempTxs.Add(transactions[i]);

                amount %= transactions[i].Amount;
            }

            var sum = Util.Sum(tempTxs.Select(s => s.Amount));
            var remainder = amount - sum;
            var closest = transactions.Select(x => x.Amount).Aggregate((x, y) => x - remainder < y - remainder ? x : y);
            var tx = transactions.FirstOrDefault(a => a.Amount.Equals(closest));

            return (tx, remainder);
        }

        /// <summary>
        /// Network address.
        /// </summary>
        /// <returns>The address.</returns>
        /// <param name="coin">Coin.</param>
        /// <param name="networkApi">Network API.</param>
        public byte[] NetworkAddress(ICoinDto coin, NetworkApiMethod networkApi = null)
        {
            Guard.Argument(coin, nameof(coin)).NotNull();

            //TODO: Will remove the need to format to and from base64..
            //try
            //{ coin = coin.FormatCoinFromBase64(); }
            //catch (FormatException) { }

            byte[] address = new byte[33];

            string env = networkApi == null ? environment : networkApi.ToString();
            address[0] = env == Constant.Mainnet ? (byte)0x1 : (byte)74;

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
                coin.Principle.FromHex());

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
            Guard.Argument(pk, nameof(pk)).NotNull().MaxCount(32);
            byte[] address = new byte[33];

            string env = networkApi == null ? environment : networkApi.ToString();
            address[0] = env == Constant.Mainnet ? (byte)0x1 : (byte)74;

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
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(version, nameof(version)).NotNegative();

            using (var insecurePassword = password.Insecure())
            {
                var hash = Cryptography.GenericHashNoKey($"{version} {insecurePassword.Value}");
                return Prover.GetHashStringNumber(hash).ToByteArray().ToHex();
            }
        }

        public IEnumerable<BlanceSheetDto> TransactionHistory(SecureString identifier, SecureString password)
        {
            ulong credit = 0;
            var session = new Session(identifier, password);

            List<TransactionDto> txns;

            using (var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString()))
            {
                txns = db.Fetch<TransactionDto>();
                if (txns?.Any() != true)
                {
                    return null;
                }
            }

            var final = txns.OrderBy(f => f.DateTime).Select(tx => new BlanceSheetDto
            {
                DateTime = tx.DateTime.ToUniversalTime(),
                Memo = tx.Memo,
                MoneyOut = tx.TransactionType == TransactionType.Send ? $"-{tx.Amount.DivWithNaT().ToString("F9")}" : "",
                MoneyIn = tx.TransactionType == TransactionType.Receive ? tx.Amount.DivWithNaT().ToString("F9") : "",
                Balance = tx.TransactionType == TransactionType.Send ? (credit -= tx.Amount).DivWithNaT().ToString("F9") : (credit += tx.Amount).DivWithNaT().ToString("F9")
            });

            return final;
        }
    }
}