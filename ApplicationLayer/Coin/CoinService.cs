// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Security;
using TangramCypher.Helper;
using TangramCypher.Helper.LibSodium;
using TangramCypher.ApplicationLayer.Actor;
using Newtonsoft.Json;
using Secp256k1_ZKP.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Coin
{
    public class CoinService : ICoinService
    {
        private double input;
        private double output;
        private double change;
        private int version;
        private string stamp;
        private SecureString password;

        /// <summary>
        /// Builds the receiver.
        /// </summary>
        /// <returns>The receiver.</returns>
        public (ReceiverOutput, CoinDto) BuildReceiver()
        {
            ReceiverOutput receiver = null;
            CoinDto coin = null;

            using (var secp256k1 = new Secp256k1())
            using (var pedersen = new Pedersen())
            {
                var blind = DeriveKey(Output());
                var blindSum = pedersen.BlindSum(new List<byte[]> { blind, blind }, new List<byte[]> { });
                var commitPos = Commit((ulong)Output(), blind);
                var commitNeg = Commit(0, blind);

                Stamp(GetNewStamp());
                Version(-1);

                coin = BuildCoin(blindSum, commitPos, commitNeg, true);
                receiver = new ReceiverOutput(Output(), commitPos, blindSum);
            }

            return (receiver, coin);
        }

        /// <summary>
        /// Builds the sender.
        /// </summary>
        /// <returns>The sender.</returns>
        public CoinDto BuildSender()
        {
            CoinDto coin = null;

            using (var secp256k1 = new Secp256k1())
            using (var pedersen = new Pedersen())
            {
                var blindPos = pedersen.BlindSwitch((ulong)Input(), DeriveKey(Input()));
                var blindNeg = pedersen.BlindSwitch((ulong)Output(), DeriveKey(Output()));
                var blindSum = pedersen.BlindSum(new List<byte[]> { blindPos }, new List<byte[]> { blindNeg });
                var commitPos = Commit((ulong)Input(), blindPos);
                var commitNeg = Commit((ulong)Output(), blindNeg);

                coin = BuildCoin(blindSum, commitPos, commitNeg);
            }

            return coin;
        }

        /// <summary>
        /// Change this instance.
        /// </summary>
        /// <returns>The change.</returns>
        public double Change() => change = Math.Abs(input) - Math.Abs(output);

        /// <summary>
        /// Clears the change, imputs, outputs and version cache.
        /// </summary>
        public void ClearCache()
        {
            change = 0;
            Input(0);
            Output(0);
            Password(null);
            Stamp(string.Empty);
            Version(0);
        }

        /// <summary>
        /// Commit the specified amount, version, stamp and password.
        /// </summary>
        /// <returns>The commit.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="version">Version.</param>
        /// <param name="stamp">Stamp.</param>
        /// <param name="password">Password.</param>
        public byte[] Commit(ulong amount, int version, string stamp, SecureString password)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentException("message", nameof(stamp));

            using (var pedersen = new Pedersen())
            {
                var blind = DeriveKey(version, stamp, password).FromHex();
                var commit = pedersen.Commit(amount, blind);

                return commit;
            }
        }

        /// <summary>
        /// Commit the specified amount.
        /// </summary>
        /// <returns>The commit.</returns>
        /// <param name="amount">Amount.</param>
        public byte[] Commit(ulong amount)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (stamp == null)
                throw new ArgumentNullException(nameof(stamp));

            using (var pedersen = new Pedersen())
            {
                var blind = DeriveKey(Version(), Stamp(), Password()).FromHex();
                var commit = pedersen.Commit(amount, blind);

                return commit;
            }
        }

        /// <summary>
        /// Commit the specified amount and blind.
        /// </summary>
        /// <returns>The commit.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="blind">Blind.</param>
        public byte[] Commit(ulong amount, byte[] blind)
        {
            if ((blind == null) && (blind.Length > 32))
                throw new ArgumentNullException(nameof(blind));

            using (var pedersen = new Pedersen())
            {
                var commit = pedersen.Commit(amount, blind);
                return commit;
            }
        }

        /// <summary>
        /// Derives the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="password">Password.</param>
        /// <param name="coin">Coin.</param>
        public CoinDto DeriveCoin(SecureString password, CoinDto coin)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            var v0 = +coin.Version;
            var v1 = +coin.Version + 1;
            var v2 = +coin.Version + 2;

            var c = new CoinDto()
            {
                Keeper = DeriveKey(v1, coin.Stamp, DeriveKey(v2, coin.Stamp, DeriveKey(v2, coin.Stamp, password).ToSecureString()).ToSecureString()),
                Version = v0,
                Principle = DeriveKey(v0, coin.Stamp, password),
                Stamp = coin.Stamp,
                Envelope = coin.Envelope,
                Hint = DeriveKey(v1, coin.Stamp, DeriveKey(v1, coin.Stamp, password).ToSecureString())
            };

            return c;
        }

        /// <summary>
        /// Derives the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="coin">Coin.</param>
        public CoinDto DeriveCoin(CoinDto coin)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            var v0 = +coin.Version;
            var v1 = +coin.Version + 1;
            var v2 = +coin.Version + 2;

            var c = new CoinDto()
            {
                Keeper = DeriveKey(v1, coin.Stamp, DeriveKey(v2, coin.Stamp, DeriveKey(v2, coin.Stamp, Password()).ToSecureString()).ToSecureString()),
                Version = v0,
                Principle = DeriveKey(v0, coin.Stamp, Password()),
                Stamp = coin.Stamp,
                Envelope = coin.Envelope,
                Hint = DeriveKey(v1, coin.Stamp, DeriveKey(v1, coin.Stamp, Password()).ToSecureString())
            };

            return c;
        }

        /// <summary>
        /// Derives the key.
        /// </summary>
        /// <returns>The key.</returns>
        /// <param name="version">Version.</param>
        /// <param name="stamp">Stamp.</param>
        /// <param name="password">Password.</param>
        /// <param name="bytes">Bytes.</param>
        public string DeriveKey(int version, string stamp, SecureString password, int bytes = 32)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentException("Stamp cannot be null or empty!", nameof(stamp));

            using (var insecurePassword = password.Insecure())
            {
                return Cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", version, stamp, insecurePassword.Value), bytes).ToHex();
            }
        }

        /// <summary>
        /// Derives the key.
        /// </summary>
        /// <returns>The key.</returns>
        /// <param name="bytes">Bytes.</param>
        public byte[] DeriveKey(int bytes = 32)
        {
            if (string.IsNullOrEmpty(Stamp()))
                throw new ArgumentException("Stamp cannot be null or empty!", nameof(stamp));

            using (var insecurePassword = Password().Insecure())
            {
                return Cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", Version(), Stamp(), insecurePassword.Value), bytes);
            }
        }

        /// <summary>
        /// Derives the key.
        /// </summary>
        /// <returns>The key.</returns>
        /// <param name="value">Value.</param>
        /// <param name="bytes">Bytes.</param>
        public byte[] DeriveKey(double value, int bytes = 32)
        {
            if (value < 0)
                throw new Exception("Value can not be less than zero!");

            using (var insecurePassword = Password().Insecure())
            {
                return Cryptography.GenericHashNoKey(string.Format("{0} {1} {2}", Version(), value, insecurePassword.Value), bytes);
            }
        }

        /// <summary>
        /// Gets the new stamp.
        /// </summary>
        /// <returns>The new stamp.</returns>
        public string GetNewStamp()
        {
            return Cryptography.GenericHashNoKey(Cryptography.RandomKey()).ToHex();
        }

        /// <summary>
        /// Hash the specified coin.
        /// </summary>
        /// <returns>The hash.</returns>
        /// <param name="coin">Coin.</param>
        public byte[] Hash(CoinDto coin)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            return Cryptography.GenericHashNoKey(
                string.Format("{0} {1} {2} {3} {4} {5} {6}",
                    coin.Envelope.Commitment,
                    coin.Envelope.Proof,
                    coin.Envelope.PublicKey,
                    coin.Hint,
                    coin.Keeper,
                    coin.Principle,
                    coin.Stamp));
        }

        /// <summary>
        ///  Releases two secret keys to continue hashchaing for sender/recipient. 
        /// </summary>
        /// <returns>The release.</returns>
        /// <param name="version">Version.</param>
        /// <param name="stamp">Stamp.</param>
        /// <param name="password">Password.</param>
        public (string, string) HotRelease(int version, string stamp, SecureString password)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentNullException(nameof(stamp));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            var key1 = DeriveKey(version + 1, stamp, password);
            var key2 = DeriveKey(version + 2, stamp, password);

            return (key1, key2);
        }

        /// <summary>
        /// Partial release one secret key for escrow.
        /// </summary>
        /// <returns>The release.</returns>
        /// <param name="version">Version.</param>
        /// <param name="stamp">Stamp.</param>
        /// <param name="memo">Memo.</param>
        /// <param name="password">Password.</param>
        public string PartialRelease(int version, string stamp, string memo, SecureString password)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentNullException(nameof(stamp));

            if (memo == null)
                throw new ArgumentNullException(nameof(memo));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            var subKey1 = DeriveKey(version + 1, stamp, password);
            var subKey2 = DeriveKey(version + 2, stamp, password).ToSecureString();
            var mix = DeriveKey(version + 2, stamp, subKey2);
            var redemption = new RedemptionKeyDto() { Key1 = subKey1, Key2 = mix, Memo = memo, Stamp = stamp };

            return JsonConvert.SerializeObject(redemption);
        }

        /// <summary>
        /// Change ownership.
        /// </summary>
        /// <returns>The swap.</returns>
        /// <param name="password">Password.</param>
        /// <param name="coin">Coin.</param>
        /// <param name="redemptionKey">Redemption key.</param>
        public (CoinDto, CoinDto) CoinSwap(SecureString password, CoinDto coin, RedemptionKeyDto redemptionKey)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            if (redemptionKey == null)
                throw new ArgumentNullException(nameof(redemptionKey));

            if (!redemptionKey.Stamp.Equals(coin.Stamp))
                throw new Exception("Redemption stamp is not equal to the coins stamp!");

            var v1 = coin.Version + 1;
            var v2 = coin.Version + 2;
            var v3 = coin.Version + 3;
            var v4 = coin.Version + 4;

            var c1 = new CoinDto()
            {
                Keeper = DeriveKey(v2, redemptionKey.Stamp, DeriveKey(v3, redemptionKey.Stamp, DeriveKey(v3, redemptionKey.Stamp, password).ToSecureString()).ToSecureString()),
                Version = v1,
                Principle = redemptionKey.Key1,
                Stamp = redemptionKey.Stamp,
                Envelope = coin.Envelope,
                Hint = DeriveKey(v2, redemptionKey.Stamp, redemptionKey.Key2.ToSecureString())
            };

            c1.Hash = Hash(c1).ToHex();

            var c2 = new CoinDto()
            {
                Keeper = DeriveKey(v3, redemptionKey.Stamp, DeriveKey(v4, redemptionKey.Stamp, DeriveKey(v4, redemptionKey.Stamp, password).ToSecureString()).ToSecureString()),
                Version = v2,
                Principle = redemptionKey.Key2,
                Stamp = redemptionKey.Stamp,
                Envelope = coin.Envelope,
                Hint = DeriveKey(v3, redemptionKey.Stamp, DeriveKey(v3, redemptionKey.Stamp, password).ToSecureString())
            };

            c2.Hash = Hash(c2).ToHex();

            return (c1, c2);
        }

        /// <summary>
        /// Change partial ownership.
        /// </summary>
        /// <returns>The partial one.</returns>
        /// <param name="password">Password.</param>
        /// <param name="coin">Coin.</param>
        /// <param name="redemptionKey">Redemption key.</param>
        public CoinDto SwapPartialOne(SecureString password, CoinDto coin, RedemptionKeyDto redemptionKey)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            if (redemptionKey == null)
                throw new ArgumentNullException(nameof(redemptionKey));

            var v1 = coin.Version + 1;
            var v2 = coin.Version + 2;
            var v3 = coin.Version + 3;

            coin.Keeper = DeriveKey(v2, coin.Stamp, DeriveKey(v3, coin.Stamp, DeriveKey(v3, coin.Stamp, password).ToSecureString()).ToSecureString());
            coin.Version = v1;
            coin.Principle = redemptionKey.Key1;
            coin.Stamp = coin.Stamp;
            coin.Envelope = coin.Envelope;
            coin.Hint = redemptionKey.Key2;

            return coin;
        }

        /// <summary>
        /// Sign the specified amount, version, stamp, password and msg.
        /// </summary>
        /// <returns>The sign.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="version">Version.</param>
        /// <param name="stamp">Stamp.</param>
        /// <param name="password">Password.</param>
        /// <param name="msg">Message.</param>
        public byte[] Sign(ulong amount, int version, string stamp, SecureString password, byte[] msg)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentNullException(nameof(stamp));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if ((msg == null) && (msg.Length > 32))
                throw new ArgumentNullException(nameof(msg));

            using (var secp256k1 = new Secp256k1())
            {
                var blind = DeriveKey(version, stamp, password).FromHex();
                var sig = secp256k1.Sign(msg, blind);

                return sig;
            }
        }

        /// <summary>
        /// Sign the specified amount and msg.
        /// </summary>
        /// <returns>The sign.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="msg">Message.</param>
        public byte[] Sign(ulong amount, byte[] msg)
        {
            if ((msg == null) && (msg.Length > 32))
                throw new ArgumentNullException(nameof(msg));

            using (var secp256k1 = new Secp256k1())
            {
                var blind = DeriveKey(Version(), Stamp(), Password()).FromHex();
                var msgHash = Cryptography.GenericHashNoKey(Encoding.UTF8.GetString(msg));
                var sig = secp256k1.Sign(msgHash, blind);

                return sig;
            }
        }

        /// <summary>
        /// Signs with blinding factor.
        /// </summary>
        /// <returns>The with blinding.</returns>
        /// <param name="msg">Message.</param>
        /// <param name="blinding">Blinding.</param>
        public byte[] SignWithBlinding(byte[] msg, byte[] blinding)
        {
            if ((msg == null) && (msg.Length > 32))
                throw new ArgumentNullException(nameof(msg));

            if ((blinding == null) && (blinding.Length > 32))
                throw new ArgumentNullException(nameof(blinding));

            using (var secp256k1 = new Secp256k1())
            {
                var msgHash = Cryptography.GenericHashNoKey(Encoding.UTF8.GetString(msg));
                return secp256k1.Sign(msgHash, blinding);
            }
        }

        /// <summary>
        /// Split the specified blinding factor. We use one of these (k1) to sign the tx_kernel (k1G)
        /// and the other gets aggregated in the block as the "offset".
        /// </summary>
        /// <returns>The split.</returns>
        /// <param name="blinding">Blinding.</param>
        public (byte[], byte[]) Split(byte[] blinding)
        {
            if ((blinding == null) && (blinding.Length > 32))
                throw new ArgumentNullException(nameof(blinding));

            using (var pedersen = new Pedersen())
            {
                var skey1 = DeriveKey(Change());
                var skey2 = pedersen.BlindSum(new List<byte[]> { blinding }, new List<byte[]> { skey1 });

                return (skey1, skey2);
            }

        }

        /// <summary>
        /// Make a single coin.
        /// </summary>
        /// <returns>The single coin.</returns>
        /// <param name="transaction">Transaction.</param>
        /// <param name="password">Password.</param>
        public CoinDto MakeSingleCoin(TransactionDto transaction, SecureString password)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            return DeriveCoin(password,
                new CoinDto
                {
                    Version = transaction.Version,
                    Stamp = transaction.Stamp,
                    Envelope = new EnvelopeDto()
                });
        }

        /// <summary>
        /// Makes the single coin.
        /// </summary>
        /// <returns>The single coin.</returns>
        public CoinDto MakeSingleCoin()
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            return DeriveCoin(new CoinDto
            {
                Version = Version() + 1,
                Stamp = Stamp(),
                Envelope = new EnvelopeDto()
            });
        }

        /// <summary>
        /// Makes multiple coins.
        /// </summary>
        /// <returns>The multiple coins.</returns>
        /// <param name="transactions">Transactions.</param>
        /// <param name="password">Password.</param>
        public IEnumerable<CoinDto> MakeMultipleCoins(IEnumerable<TransactionDto> transactions, SecureString password)
        {
            if (transactions == null)
                throw new ArgumentNullException(nameof(transactions));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            return transactions.Select(tx =>
                DeriveCoin(password, new CoinDto
                {
                    Version = tx.Version,
                    Stamp = tx.Stamp,
                    Envelope = new EnvelopeDto()
                }));
        }

        /// <summary>
        /// Inputs this instance.
        /// </summary>
        /// <returns>The inputs.</returns>
        public double Input() => input;

        /// <summary>
        /// Input the specified value.
        /// </summary>
        /// <returns>The input.</returns>
        /// <param name="value">Value.</param>
        public CoinService Input(double value)
        {
            if (value < 0)
                throw new Exception("Value can not be less than zero!");

            input = value;

            return this;
        }

        /// <summary>
        /// Outputs this instance.
        /// </summary>
        /// <returns>The outputs.</returns>
        public double Output() => output;

        /// <summary>
        /// Output the specified value.
        /// </summary>
        /// <returns>The output.</returns>
        /// <param name="value">Value.</param>
        public CoinService Output(double value)
        {
            if (value < 0)
                throw new Exception("Value can not be less than zero!");

            output = value;

            return this;
        }

        /// <summary>
        /// Verifies the coin on ownership.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="terminal">Terminal.</param>
        /// <param name="current">Current.</param>
        public int VerifyCoin(CoinDto terminal, CoinDto current)
        {
            if (terminal == null)
                throw new ArgumentNullException(nameof(terminal));

            if (current == null)
                throw new ArgumentNullException(nameof(current));

            return terminal.Keeper.Equals(current.Keeper) && terminal.Hint.Equals(current.Hint)
               ? 1
               : terminal.Hint.Equals(current.Hint)
               ? 2
               : terminal.Keeper.Equals(current.Keeper)
               ? 3
               : 4;
        }

        /// <summary>
        /// Version this instance.
        /// </summary>
        /// <returns>The version.</returns>
        public int Version() => version;

        /// <summary>
        /// Version the specified version.
        /// </summary>
        /// <returns>The version.</returns>
        /// <param name="version">Version.</param>
        public CoinService Version(int version)
        {
            this.version = version;
            return this;
        }

        /// <summary>
        /// Stamp this instance.
        /// </summary>
        /// <returns>The stamp.</returns>
        public string Stamp() => stamp;

        /// <summary>
        /// Stamp the specified stamp.
        /// </summary>
        /// <returns>The stamp.</returns>
        /// <param name="stamp">Stamp.</param>
        public CoinService Stamp(string stamp)
        {
            this.stamp = stamp;
            return this;
        }

        /// <summary>
        /// Password this instance.
        /// </summary>
        /// <returns>The password.</returns>
        public SecureString Password() => password;

        /// <summary>
        /// Password the specified password.
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="password">Password.</param>
        public CoinService Password(SecureString password)
        {
            this.password = password;
            return this;
        }

        /// <summary>
        /// Builds the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="blindSum">Blind sum.</param>
        /// <param name="commitPos">Commit position.</param>
        /// <param name="commitNeg">Commit neg.</param>
        private CoinDto BuildCoin(byte[] blindSum, byte[] commitPos, byte[] commitNeg, bool receiver = false)
        {
            if ((blindSum == null) && (blindSum.Length > 32))
                throw new ArgumentNullException(nameof(blindSum));

            if ((commitPos == null) && (commitPos.Length > 33))
                throw new ArgumentNullException(nameof(commitPos));

            if ((commitNeg == null) && (commitNeg.Length > 33))
                throw new ArgumentNullException(nameof(commitNeg));

            CoinDto coin;
            bool isVerified;

            using (var secp256k1 = new Secp256k1())
            using (var pedersen = new Pedersen())
            {
                var commitSum = pedersen.CommitSum(new List<byte[]> { commitPos }, new List<byte[]> { commitNeg });

                isVerified = receiver
                    ? pedersen.VerifyCommitSum(new List<byte[]> { commitPos, commitNeg }, new List<byte[]> { Commit((ulong)Output(), blindSum) })
                    : pedersen.VerifyCommitSum(new List<byte[]> { commitPos }, new List<byte[]> { commitNeg, commitSum });

                if (!isVerified)
                    throw new Exception(nameof(isVerified));

                var (k1, k2) = Split(blindSum);

                coin = MakeSingleCoin();

                coin.Envelope.Commitment = commitSum.ToHex();
                coin.Envelope.Proof = k2.ToHex();
                coin.Envelope.PublicKey = pedersen.ToPublicKey(Commit(0, k1)).ToHex();
                coin.Envelope.Signature = secp256k1.Sign(Hash(coin), k1).ToHex();

                coin.Hash = Hash(coin).ToHex();
            }

            return coin;
        }
    }
}
