// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using TGMWalletCore.Helper;
using TGMWalletCore.LibSodium;
using TGMWalletCore.Actor;
using Microsoft.Extensions.Configuration;
using Dawn;
using Microsoft.Extensions.Logging;
using TGMWalletCore.Model;
using System.IO;
using Util = TGMWalletCore.Helper.Util;
using Transaction = TGMWalletCore.Model.Transaction;
using Constant = TGMWalletCore.Actor.Constant;
using NBitcoin;
using System.Threading.Tasks;

namespace TGMWalletCore.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly ILogger _logger;
        private readonly Network _network;

        public WalletService(IConfiguration configuration, ILogger<WalletService> logger)
        {
            var apiNetworkSection = configuration.GetSection(Constant.ApiNetwork);
            var environment = apiNetworkSection.GetValue<string>(Constant.Environment);

            _network = environment == Constant.Mainnet ? Network.Main : Network.TestNet;
            _logger = logger;
        }

        /// <summary>
        /// Gets the generic available balance.
        /// </summary>
        /// <returns>The balance.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="passphrase">passphrase.</param>
        public TaskResult<ulong> AvailableBalance(SecureString identifier, SecureString passphrase)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            ulong balance;

            try
            {
                using var db = Util.LiteRepositoryFactory(passphrase, identifier.ToUnSecureString());
                var txns = db.Query<Transaction>().ToList();
                if (txns?.Any() != true)
                {
                    return TaskResult<ulong>.CreateSuccess(0);
                }

                balance = Balance(txns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<ulong>.CreateFailure(ex);
            }

            return TaskResult<ulong>.CreateSuccess(balance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="identifier"></param>
        public void AddKeySet(SecureString identifier, SecureString passphrase)
        {
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();
            Guard.Argument(identifier, nameof(identifier)).NotNull();

            try
            {
                using var db = Util.LiteRepositoryFactory(passphrase, identifier.ToUnSecureString());

                var next = NextKeySet(identifier, passphrase);
                var keypth = new KeyPath(next.Paths[1]);
                var keySet = CreateKeySet(keypth.ToString(), next.RootKey.FromHexString(), next.ChainCode.FromHexString());

                db.Insert(keySet);

                next.ChainCode.ZeroString();
                next.RootKey.ZeroString();

                keySet.RootKey.ZeroString();
                keySet.ChainCode.ZeroString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="secKey"></param>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        public KeySet CreateKeySet(string path, byte[] secKey, byte[] chainCode)
        {
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();
            Guard.Argument(secKey, nameof(secKey)).NotNull().MaxCount(32);
            Guard.Argument(chainCode, nameof(chainCode)).NotNull().MaxCount(32);

            var masterKey = new ExtKey(new Key(secKey), chainCode);

            var keyPaths = new string[2];
            keyPaths[0] = IncrementKeyPath(path).ToString();
            keyPaths[1] = IncrementKeyPath(keyPaths[0]).ToString();

            var spend = masterKey.Derive(new KeyPath(keyPaths[0])).PrivateKey;
            var scan = masterKey.Derive(new KeyPath(keyPaths[1])).PrivateKey;

            return new KeySet
            {
                ChainCode = masterKey.ChainCode.ToHexString(),
                Paths = keyPaths,
                RootKey = masterKey.PrivateKey.ToHex(),
                StealthAddress = spend.PubKey.CreateStealthAddress(scan.PubKey, _network).ToString()
            };
        }

        /// <summary>
        /// Create new wallet.
        /// </summary>
        /// <returns>The wallet.</returns>
        public string CreateWallet(SecureString mnemonic, SecureString passphrase)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            var walletId = NewID(16);

            walletId.MakeReadOnly();
            mnemonic.MakeReadOnly();
            passphrase.MakeReadOnly();

            CreateHDRootKey(mnemonic, passphrase, out string concatenateMnemonic, out ExtKey hdRoot);

            concatenateMnemonic.ZeroString();

            var keySet = CreateKeySet("m/44'/271'/0'/0", hdRoot.PrivateKey.ToHex().FromHexString(), hdRoot.ChainCode);

            try
            {
                using (var db = Util.LiteRepositoryFactory(passphrase, walletId.ToUnSecureString()))
                {
                    db.Insert(keySet);
                }

                keySet.ChainCode.ZeroString();
                keySet.RootKey.ZeroString();

                return walletId.ToUnSecureString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception("Failed to create wallet.");
            }
            finally
            {
                walletId.Dispose();
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
            foreach (var c in $"id_{Crypto.RandomBytes(bytes).ToHexString()}") secureString.AppendChar(c);
            return secureString;
        }

        /// <summary>
        /// BIP39 mnemonic.
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> CreateMnemonic(Language language, WordCount wordCount)
        {
            var wordList = await Wordlist.LoadWordList(language);
            var mnemo = new Mnemonic(wordList, wordCount);

            return mnemo.Words;
        }

        /// <summary>
        /// Hashs the passphrase.
        /// </summary>
        /// <returns>The passphrase.</returns>
        /// <param name="passphrase">Passphrase.</param>
        public byte[] HashPassphrase(SecureString passphrase) => Crypto.ArgonHashString(passphrase);

        /// <summary>
        /// Gets the total transaction amount.
        /// </summary>
        /// <returns>The transaction amount.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="passphrase">Passphrase.</param>
        /// <param name="stamp">Stamp.</param>
        public ulong TotalTransactionAmount(SecureString identifier, SecureString passphrase, string address)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            ulong total;

            using (var db = Util.LiteRepositoryFactory(passphrase, identifier.ToUnSecureString()))
            {
                var txns = db.Query<Transaction>().Where(x => x.Address == address).ToEnumerable();
                if (txns?.Any() != true)
                {
                    return 0;
                }

                var outputs = txns.Select(a => a.Output);
                total = Util.Sum(outputs);
            }

            return total;
        }

        /// <summary>
        /// Last transaction amount.
        /// </summary>
        /// <returns>The transaction amount.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="passphrase">Passphrase.</param>
        public Transaction LastTransaction(SecureString identifier, SecureString passphrase, TransactionType transactionType)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            Transaction transaction;

            using (var db = Util.LiteRepositoryFactory(passphrase, identifier.ToUnSecureString()))
            {
                var txns = db.Query<Transaction>().ToList();
                if (txns?.Any() != true)
                {
                    return null;
                }

                transaction = txns.Last(tx => tx.TransactionType == transactionType);
            }

            return transaction;
        }

        /// <summary>
        /// Sorts the change.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public TaskResult<Transaction> SortChange(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            Transaction transaction = null;

            try
            {
                List<Transaction> txns;

                using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
                {
                    txns = db.Query<Transaction>().Where(x => x.Address == session.SenderAddress).ToList();
                    if (txns?.Any() != true)
                    {
                        return null;
                    }
                }

                var txsIn = txns.Where(tx => tx.TransactionType == TransactionType.Receive).ToArray();
                var target = new Transaction[txsIn.Length];

                Array.Copy(txsIn, target, txsIn.Length);

                for (int i = 0, targetLength = target.Length; i < targetLength; i++)
                {
                    var balance = Balance(txns);
                    if (balance >= session.Amount)
                    {
                        transaction = new Transaction
                        {
                            Address = session.SenderAddress,
                            Balance = balance,
                            DateTime = DateTime.Now,
                            EphemKey = txsIn[i].EphemKey,
                            Input = session.Amount,
                            Memo = session.Memo,
                            Output = balance - session.Amount,
                            Spent = transaction.Output == 0,
                            TransactionId = session.SessionId
                        };

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<Transaction>.CreateFailure(ex);
            }

            return TaskResult<Transaction>.CreateSuccess(transaction);
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

        /// <summary>
        /// Lists all KeySets
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IEnumerable<KeySet> ListKeySets(SecureString identifier, SecureString passphrase)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            using var db = Util.LiteRepositoryFactory(passphrase, identifier.ToUnSecureString());
            var keys = db.Query<KeySet>().ToList();
            if (keys?.Any() != true)
            {
                return Enumerable.Empty<KeySet>();
            }

            return keys;
        }

        /// <summary>
        /// Lists all addresses
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IEnumerable<string> ListAddresses(SecureString identifier, SecureString passphrase)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();


            var keys = ListKeySets(identifier, passphrase);
            if (keys?.Any() != true)
            {
                return Enumerable.Empty<string>();
            }

            return keys.Select(k => k.StealthAddress);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static KeyPath IncrementKeyPath(string path)
        {
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            var keypth = new KeyPath(path);
            return keypth.Increment();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="passphrase"></param>
        /// <param name="concatenateMnemonic"></param>
        /// <param name="hdRoot"></param>
        private void CreateHDRootKey(SecureString mnemonic, SecureString passphrase, out string concatenateMnemonic, out ExtKey hdRoot)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            concatenateMnemonic = string.Join(" ", mnemonic.ToUnSecureString());
            hdRoot = new Mnemonic(concatenateMnemonic).DeriveExtKey(passphrase.ToUnSecureString());
        }

        /// <summary>
        /// Calculate balance from transactions.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private ulong Balance(IEnumerable<Transaction> source)
        {
            Guard.Argument(source, nameof(source)).NotNull();

            ulong total = 0UL;
            ulong? pocket = null;
            ulong? burnt = null;

            try
            {
                pocket = Util.Sum(source, TransactionType.Receive);
                burnt = Util.Sum(source, TransactionType.Send);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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

            return total;
        }

        /// <summary>
        /// Calculates the change.
        /// </summary>
        /// <returns>The change.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="transactions">Transactions.</param>
        private (Transaction, ulong) CalculateChange(ulong amount, Transaction[] transactions)
        {
            Guard.Argument(transactions, nameof(transactions)).NotNull();

            int count;
            var tempTxs = new List<Transaction>();

            for (var i = 0; i < transactions.Length; i++)
            {
                count = (int)(amount / transactions[i].Output);
                if (count != 0)
                    for (int k = 0; k < count; k++) tempTxs.Add(transactions[i]);

                amount %= transactions[i].Output;
            }

            var sum = Util.Sum(tempTxs.Select(s => s.Output));
            var remainder = amount - sum;
            var closest = transactions.Select(x => x.Output).Aggregate((x, y) => x - remainder < y - remainder ? x : y);
            var tx = transactions.FirstOrDefault(a => a.Output == closest);

            return (tx, remainder);
        }

        /// <summary>
        /// Returns balance sheet for the calling wallet.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public IEnumerable<BlanceSheet> TransactionHistory(SecureString identifier, SecureString passphrase)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            ulong credit = 0;
            var session = new Session(identifier, passphrase);

            List<Transaction> txns;

            using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
            {
                txns = db.Query<Transaction>().ToList();
                if (txns?.Any() != true)
                {
                    return null;
                }
            }

            var final = txns.OrderBy(f => f.DateTime).Select(tx => new BlanceSheet
            {
                DateTime = tx.DateTime.ToUniversalTime(),
                Memo = tx.Memo,
                MoneyOut = tx.TransactionType == TransactionType.Send ? $"-{tx.Output.DivWithNaT():F9}" : "",
                MoneyIn = tx.TransactionType == TransactionType.Receive ? tx.Output.DivWithNaT().ToString("F9") : "",
                Balance = tx.TransactionType == TransactionType.Send ? (credit -= tx.Output).DivWithNaT().ToString("F9") : (credit += tx.Output).DivWithNaT().ToString("F9")
            });

            return final;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public KeySet NextKeySet(SecureString identifier, SecureString passphrase)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            var session = new Session(identifier, passphrase);

            using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());
            var keySet = db.Query<KeySet>().ToList().Last();

            return keySet;
        }
    }
}