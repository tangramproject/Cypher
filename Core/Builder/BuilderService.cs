// Core (c) by Tangram Inc
//
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using Tangram.Core.Helper;
using Tangram.Core.LibSodium;
using Tangram.Core.Actor;
using Microsoft.Extensions.Logging;
using Tangram.Core.Model;
using Secp256k1ZKP.Net;
using Newtonsoft.Json.Linq;
using NBitcoin.Stealth;
using NBitcoin;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Constant = Tangram.Core.Actor.Constant;
using Transaction = Tangram.Core.Model.Transaction;

namespace Tangram.Core.Coin
{
    public unsafe class BuilderService : IBuilderService
    {
        private readonly ILogger _logger;
        private int _indexOfBalance;
        private readonly Network _network;

        public BuilderService(IConfiguration configuration, ILogger<BuilderService> logger)
        {
            var apiNetworkSection = configuration.GetSection(Constant.ApiNetwork);
            var environment = apiNetworkSection.GetValue<string>(Constant.Environment);

            _network = environment == Constant.Mainnet ? Network.Main : Network.TestNet;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public TaskResult<Model.Coin> Build(Session session, Transaction transaction)
        {
            using var secp256k1 = new Secp256k1();
            using var pedersen = new Pedersen();
            using var mlsag = new MLSAG();
            using var bulletProof = new BulletProof();

            var blinds = new Span<byte[]>(new byte[3][]);
            var sk = new Span<byte[]>(new byte[2][]);
            int nRows = 2; // last row sums commitments
            int nCols = 17; // ring size
            int index = Secp256k1ZKP.Net.Util.Rand(1, 17) % nCols;
            var m = new byte[nRows * nCols * 33];
            var pcm_in = new Span<byte[]>(new byte[nCols * 1][]);
            var pcm_out = new Span<byte[]>(new byte[2][]);
            var randSeed = secp256k1.Randomize32();
            var preimage = secp256k1.Randomize32();
            var pc = new byte[32];
            var ki = new byte[33 * 1];
            var ss = new byte[nCols * nRows * 32];

            var blindInput = secp256k1.CreatePrivateKey();
            var blindChange = secp256k1.CreatePrivateKey();

            blinds[1] = blindInput;
            blinds[2] = blindChange;

            var commitInput = pedersen.Commit(transaction.Input, blindInput);
            var commitChange = pedersen.Commit(transaction.Output, blindChange);

            pcm_out[0] = commitInput;
            pcm_out[1] = commitChange;


            m = M(session, transaction, secp256k1, pedersen, blinds, sk, nRows, nCols, index, m, pcm_in);

            var blindSum = new byte[32];
            var success = mlsag.Prepare(m, blindSum, 2, 2, nCols, nRows, pcm_in, pcm_out, blinds);

            if (!success)
            {
                return TaskResult<Model.Coin>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Prepare failed."
                }));
            }

            sk[nRows - 1] = blindSum;

            success = mlsag.Generate(ki, pc, ss, randSeed, preimage, nCols, nRows, index, sk, m);

            if (!success)
            {
                return TaskResult<Model.Coin>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Generate failed."
                }));
            }

            success = mlsag.Verify(preimage, nCols, nRows, m, ki, pc, ss);

            if (!success)
            {
                return TaskResult<Model.Coin>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Verify failed."
                }));
            }

            var coin = new Model.Coin
            {
                Mix = nCols,
                PreImage = preimage.ToHexString(),
                Ver = 0x1,
                Vin = new Vin()
                {
                    K = ki.ToHexString(),
                    M = m.ToHexString(),
                    P = pc.ToHexString(),
                    S = ss.ToHexString()
                }
            };

            var vouts = new Vout(pcm_out.Length);

            var bullet = BulletProof(transaction.Input, blindInput, commitInput);

            if (!bullet.Success)
            {
                return TaskResult<Model.Coin>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = bullet.Exception.Message
                }));
            }

            var receiver = GenStealthPayment(session.RecipientAddress);

            vouts.C[0] = commitInput.ToHexString();
            vouts.E[0] = receiver.metadata.ToHex();
            vouts.N[0] = receiver.paymentAddress.Encrypt(JsonConvert.SerializeObject(new TransactionMessage
            {
                Amount = transaction.Input,
                Blind = blindInput.ToHexString()
            }));
            vouts.P[0] = receiver.paymentAddress.ToHex();
            vouts.R[0] = bullet.Result.proof.ToHexString();

            bullet = BulletProof(transaction.Output, blindChange, commitChange);

            if (!bullet.Success)
            {
                return TaskResult<Model.Coin>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = bullet.Exception.Message
                }));
            }

            var sender = GenStealthPayment(session.SenderAddress);

            vouts.C[1] = commitChange.ToHexString();
            vouts.E[1] = sender.metadata.ToHex();
            vouts.N[1] = sender.paymentAddress.Encrypt(JsonConvert.SerializeObject(new TransactionMessage
            {
                Amount = transaction.Output,
                Blind = blindChange.ToHexString()
            }));
            vouts.P[1] = sender.paymentAddress.ToHex();
            vouts.R[1] = bullet.Result.proof.ToHexString();

            coin.Vout = vouts;

            return TaskResult<Model.Coin>.CreateSuccess(coin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="purchase"></param>
        /// <param name="secp256k1"></param>
        /// <param name="pedersen"></param>
        /// <param name="blinds"></param>
        /// <param name="sk"></param>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>
        /// <param name="index"></param>
        /// <param name="m"></param>
        /// <param name="pcm_in"></param>
        /// <returns></returns>
        private byte[] M(Session session, Transaction transaction, Secp256k1 secp256k1, Pedersen pedersen,
            Span<byte[]> blinds, Span<byte[]> sk, int nRows, int nCols, int index, byte[] m, Span<byte[]> pcm_in)
        {
            for (int k = 0; k < nRows - 1; ++k)
                for (int i = 0; i < nCols; ++i)
                {
                    if (i == index)
                    {
                        var keySet = GetKeySet(session);
                        var masterKey = new ExtKey(new Key(keySet.RootKey.FromHexString()), keySet.ChainCode.FromHexString());
                        var spendKey = masterKey.Derive(new KeyPath(keySet.Paths[0])).PrivateKey;
                        var scanKey = masterKey.Derive(new KeyPath(keySet.Paths[1])).PrivateKey;
                        var generatedKey = spendKey.Uncover(scanKey, new PubKey(transaction.EphemKey.FromHexString()));

                        sk[0] = generatedKey.ToHex().FromHexString();
                        blinds[0] = secp256k1.CreatePrivateKey();
                        _indexOfBalance = i + k * nCols;
                        pcm_in[_indexOfBalance] = pedersen.Commit(transaction.Balance, blinds[0]);

                        fixed (byte* mm = m, pk = generatedKey.PubKey.ToBytes())
                        {
                            Secp256k1ZKP.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                        }

                        keySet.ChainCode.ZeroString();
                        keySet.RootKey.ZeroString();

                        continue;
                    }

                    // Make fake inputs. Should collect outputs as the new imputs from network.
                    var fakeAmountIn = 1;
                    pcm_in[i + k * nCols] = pedersen.Commit((ulong)fakeAmountIn, secp256k1.Randomize32());

                    fixed (byte* mm = m, pk = secp256k1.CreatePublicKey(secp256k1.Randomize32(), true))
                    {
                        Secp256k1ZKP.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                    }
                }

            return m;
        }

        /// <summary>
        /// Bulletproof commitment.
        /// </summary>
        /// <param name="balance"></param>
        /// <param name="blindSum"></param>
        /// <param name="commitSum"></param>
        /// <returns></returns>
        private TaskResult<ProofStruct> BulletProof(ulong balance, byte[] blindSum, byte[] commitSum)
        {
            ProofStruct proofStruct;

            try
            {
                using var bulletProof = new BulletProof();

                proofStruct = bulletProof.ProofSingle(balance, blindSum, Crypto.RandomBytes(), null, null, null);
                var success = bulletProof.Verify(commitSum, proofStruct.proof, null);

                if (!success)
                {
                    return TaskResult<ProofStruct>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Bulletproof Verify failed."
                    }));
                }
            }
            catch (Exception ex)
            {
                return TaskResult<ProofStruct>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<ProofStruct>.CreateSuccess(proofStruct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private (PubKey paymentAddress, PubKey metadata) GenStealthPayment(string address)
        {
            try
            {
                var ephem = new Key();
                var stealth = new BitcoinStealthAddress(address, _network);
                var pubKey = new PubKey(stealth.SpendPubKeys[0].ToBytes());
                var uncover = pubKey.UncoverSender(ephem, stealth.ScanPubKey);

                return (uncover, ephem.PubKey);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private KeySet GetKeySet(Session session)
        {
            using var db = Helper.Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());
            var keySet = db.Query<KeySet>().Where(k => k.StealthAddress.Equals(session.SenderAddress)).FirstOrDefault();

            return keySet;
        }
    }
}