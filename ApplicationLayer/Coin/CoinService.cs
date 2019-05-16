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
using Dawn;

namespace TangramCypher.ApplicationLayer.Coin
{
    public class CoinService : ICoinService
    {
        public const int Tan = 1;
        public const int MicroTan = 100;
        public const int NanoTan = 1000_000_000;
        public const long AttoTan = 1000_000_000_000_000_000;

        private double input;
        private double output;
        private double change;
        private int version;
        private string stamp;
        private SecureString password;
        private ReceiverOutput receiverOutput;
        private CoinDto mintedCoin;
        private ProofStruct proofStruct;

        /// <summary>
        /// Builds the receiver.
        /// </summary>
        /// <returns>The receiver.</returns>
        public CoinService BuildReceiver()
        {
            using (var secp256k1 = new Secp256k1())
            using (var pedersen = new Pedersen())
            {
                var naTOutput = NaT(Output());
                var blind = DeriveKey(naTOutput);
                var blindSum = pedersen.BlindSum(new List<byte[]> { blind, blind }, new List<byte[]> { });
                var commitPos = Commit(naTOutput, blind);
                var commitNeg = Commit(0, blind);

                Stamp(GetNewStamp());
                Version(-1);

                mintedCoin = BuildCoin(blindSum, commitPos, commitNeg, true);
                receiverOutput = new ReceiverOutput(Output(), commitPos, blindSum);
            }

            return this;
        }

        /// <summary>
        /// Builds the sender.
        /// </summary>
        /// <returns>The sender.</returns>
        public CoinService BuildSender()
        {
            using (var secp256k1 = new Secp256k1())
            using (var pedersen = new Pedersen())
            {
                var naTInput = NaT(Input());
                var naTOutput = NaT(Output());
                var blindPos = pedersen.BlindSwitch(naTInput, DeriveKey(naTInput));
                var blindNeg = pedersen.BlindSwitch(naTOutput, DeriveKey(naTOutput));
                var blindSum = pedersen.BlindSum(new List<byte[]> { blindPos }, new List<byte[]> { blindNeg });
                var commitPos = Commit(naTInput, blindPos);
                var commitNeg = Commit(naTOutput, blindNeg);

                mintedCoin = BuildCoin(blindSum, commitPos, commitNeg);
            }

            return this;
        }

        /// <summary>
        /// Change this instance.
        /// </summary>
        /// <returns>The change.</returns>
        public double Change() => change = Math.Abs(input) - Math.Abs(output);

        /// <summary>
        /// Clears the change, imputs, minted coin, outputs, password, receiver output, stamp and version cache.
        /// </summary>
        public void ClearCache()
        {
            change = 0;
            Input(0);
            mintedCoin = null;
            Output(0);
            Password(null);
            receiverOutput = null;
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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(blind, nameof(blind)).NotNull().MaxCount(32);

            using (var pedersen = new Pedersen())
            {
                var commit = pedersen.Commit(amount, blind);
                return commit;
            }
        }

        /// <summary>
        /// Coin.
        /// </summary>
        /// <returns>The coin.</returns>
        public CoinDto Coin() => mintedCoin;

        /// <summary>
        /// Derives the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="password">Password.</param>
        /// <param name="coin">Coin.</param>
        public CoinDto DeriveCoin(SecureString password, CoinDto coin)
        {
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(coin, nameof(coin)).NotNull();

            var v0 = +coin.Version;
            var v1 = +coin.Version + 1;
            var v2 = +coin.Version + 2;

            var coinDto = new CoinDto()
            {
                Keeper = DeriveKey(v1, coin.Stamp, DeriveKey(v2, coin.Stamp, DeriveKey(v2, coin.Stamp, password).ToSecureString()).ToSecureString()),
                Version = v0,
                Principle = DeriveKey(v0, coin.Stamp, password),
                Stamp = coin.Stamp,
                Envelope = coin.Envelope,
                Hint = DeriveKey(v1, coin.Stamp, DeriveKey(v1, coin.Stamp, password).ToSecureString())
            };

            return coinDto;
        }

        /// <summary>
        /// Derives the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="coin">Coin.</param>
        public CoinDto DeriveCoin(CoinDto coin)
        {
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(coin, nameof(coin)).NotNull();

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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(value, nameof(value)).NotNegative();
            Guard.Argument(password, nameof(password)).NotNull();

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
        /// Gets the receivers output.
        /// </summary>
        /// <returns>The output.</returns>
        public ReceiverOutput ReceiverOutput() => receiverOutput;

        /// <summary>
        /// Hash the specified coin.
        /// </summary>
        /// <returns>The hash.</returns>
        /// <param name="coin">Coin.</param>
        public byte[] Hash(CoinDto coin)
        {
            Guard.Argument(coin, nameof(coin)).NotNull();

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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(coin, nameof(coin)).NotNull();
            Guard.Argument(redemptionKey, nameof(redemptionKey)).NotNull();

            try
            { coin = coin.FormatCoinFromBase64(); }
            catch (FormatException) { }

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
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(coin, nameof(coin)).NotNull();
            Guard.Argument(redemptionKey, nameof(redemptionKey)).NotNull();

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
            Guard.Argument(stamp, nameof(stamp)).NotNull().NotEmpty();
            Guard.Argument(password, nameof(password)).NotNull();
            Guard.Argument(msg, nameof(msg)).NotNull().MaxCount(32);

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
            Guard.Argument(msg, nameof(msg)).NotNull().MaxCount(32);

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
            Guard.Argument(msg, nameof(msg)).NotNull().MaxCount(32);
            Guard.Argument(blinding, nameof(blinding)).NotNull().MaxCount(32);

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
        public (byte[], byte[]) Split(byte[] blinding, bool isReceiver = false)
        {
            Guard.Argument(blinding, nameof(blinding)).NotNull().MaxCount(32);

            using (var pedersen = new Pedersen())
            {
                ulong naTInput = 0, naTOutput = 0, naTChange = 0;

                if (isReceiver)
                    naTChange = NaT(Output());
                else
                {
                    naTInput = NaT(Input());
                    naTOutput = NaT(Output());
                    naTChange = naTInput - naTOutput;
                }

                var skey1 = DeriveKey(naTChange);
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
            Guard.Argument(transaction, nameof(transaction)).NotNull();
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(password, nameof(password)).NotNull();

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
            Guard.Argument(transactions, nameof(transactions)).NotNull();
            Guard.Argument(password, nameof(password)).NotNull();

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
            input = Guard.Argument(value, nameof(value)).NotNegative();
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
            output = Guard.Argument(value, nameof(value)).NotNegative();
            return this;
        }

        /// <summary>
        /// Range proof struct.
        /// </summary>
        /// <returns>The struct.</returns>
        public ProofStruct ProofStruct() => proofStruct;

        /// <summary>
        /// Verifies the coin on ownership.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="terminal">Terminal.</param>
        /// <param name="current">Current.</param>
        public int VerifyCoin(CoinDto terminal, CoinDto current)
        {
            Guard.Argument(terminal, nameof(terminal)).NotNull();
            Guard.Argument(current, nameof(current)).NotNull();

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
        private CoinDto BuildCoin(byte[] blindSum, byte[] commitPos, byte[] commitNeg, bool isReceiver = false)
        {
            Guard.Argument(blindSum, nameof(blindSum)).NotNull().MaxCount(32);
            Guard.Argument(commitPos, nameof(commitPos)).NotNull().MaxCount(33);
            Guard.Argument(commitNeg, nameof(commitNeg)).NotNull().MaxCount(33);

            CoinDto coin;
            bool isVerified;

            using (var secp256k1 = new Secp256k1())
            using (var pedersen = new Pedersen())
            using (var rangeProof = new RangeProof())
            {
                var commitSum = pedersen.CommitSum(new List<byte[]> { commitPos }, new List<byte[]> { commitNeg });
                var naTInput = NaT(Input());
                var naTOutput = NaT(Output());
                var naTChange = naTInput - naTOutput;

                isVerified = isReceiver
                    ? pedersen.VerifyCommitSum(new List<byte[]> { commitPos, commitNeg }, new List<byte[]> { Commit(naTOutput, blindSum) })
                    : pedersen.VerifyCommitSum(new List<byte[]> { commitPos }, new List<byte[]> { commitNeg, commitSum });

                if (!isVerified)
                    throw new ArgumentOutOfRangeException(nameof(isVerified), "Verify commit sum failed.");

                var (k1, k2) = Split(blindSum, isReceiver);

                coin = MakeSingleCoin();

                coin.Envelope.Commitment = isReceiver ? Commit(naTOutput, blindSum).ToHex() : commitSum.ToHex();
                coin.Envelope.Proof = k2.ToHex();
                coin.Envelope.PublicKey = pedersen.ToPublicKey(Commit(0, k1)).ToHex();
                coin.Envelope.Signature = secp256k1.Sign(Hash(coin), k1).ToHex();

                coin.Hash = Hash(coin).ToHex();

                proofStruct = isReceiver
                    ? rangeProof.Proof(0, naTOutput, blindSum, coin.Envelope.Commitment.FromHex(), coin.Hash.FromHex())
                    : rangeProof.Proof(0, naTChange, blindSum, coin.Envelope.Commitment.FromHex(), coin.Hash.FromHex());

                isVerified = rangeProof.Verify(coin.Envelope.Commitment.FromHex(), proofStruct);

                if (!isVerified)
                    throw new ArgumentOutOfRangeException(nameof(isVerified), "Range proof failed.");
            }

            return coin;
        }

        /// <summary>
        /// naT decimal format.
        /// </summary>
        /// <returns>The t.</returns>
        /// <param name="value">Value.</param>
        private ulong NaT(double value)
        {
            return (ulong)(value * NanoTan);
        }
    }
}
