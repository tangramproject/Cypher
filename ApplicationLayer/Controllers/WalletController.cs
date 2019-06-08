// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helper;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController
    {
        private readonly IActorService actorService;
        private readonly IWalletService walletService;
        private readonly IVaultServiceClient vaultServiceClient;
        private readonly IUnitOfWork unitOfWork;

        public WalletController(IActorService actorService, IWalletService walletService, IVaultServiceClient vaultServiceClient, IUnitOfWork unitOfWork)
        {
            this.actorService = actorService;
            this.walletService = walletService;
            this.vaultServiceClient = vaultServiceClient;
            this.unitOfWork = unitOfWork;
        }

        [HttpPost("address", Name = "CreateWalletAddress")]
        public async Task<IActionResult> CreateWalletAddress([FromBody] CredentialsDto credentials)
        {
            var keySet = walletService.CreateKeySet();
            var added = await unitOfWork.GetKeySetRepository().Put(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString(), StoreKey.AddressKey, keySet.Address, keySet);

            if (added)
                return new CreatedResult("httpWallet", new { success = added });

            return new BadRequestResult();
        }

        [HttpPost("balance", Name = "WalletBalance")]
        public async Task<IActionResult> WalletBalance([FromBody] CredentialsDto credentials)
        {
            var total = await walletService.AvailableBalance(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            return new OkObjectResult(new { balance = total });
        }

        [HttpGet("create", Name = "CreateWallet")]
        public async Task<IActionResult> CreateWallet()
        {
            var creds = await walletService.CreateWallet();
            return new OkObjectResult(creds);
        }

        [HttpPost("profile", Name = "WalletProfile")]
        public async Task<IActionResult> WalletProfile([FromBody] CredentialsDto credentials)
        {
            var profile = await walletService.Profile(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            return new OkObjectResult(JsonConvert.DeserializeObject(profile));
        }

        [HttpGet("list", Name = "WalletList")]
        public async Task<IActionResult> WalletList()
        {
            var list = await walletService.WalletList();
            return new OkObjectResult(list);
        }

        [HttpPost("receive", Name = "WalletReceivePayment")]
        public async Task<IActionResult> WalletReceivePayment([FromBody] ReceivePaymentDto receivePaymentDto)
        {
            double balance = 0d;

            try
            {
                // if (receivePaymentDto.RedemptionMessage != null)
                // {
                //     await actorService
                //       .ReceivePaymentRedemptionKey(JsonConvert.SerializeObject(receivePaymentDto.RedemptionMessage));
                // }
                // else
                // {
                //     await actorService.ReceivePayment();
                // }

                balance = await walletService.AvailableBalance(receivePaymentDto.Credentials.Identifier.ToSecureString(), receivePaymentDto.Credentials.Password.ToSecureString());
            }
            catch (Exception ex)
            {
                balance = await walletService.AvailableBalance(receivePaymentDto.Credentials.Identifier.ToSecureString(), receivePaymentDto.Credentials.Password.ToSecureString());
                return new ObjectResult(new { error = ex.Message, statusCode = 500, balance = balance });
            }

            return new OkObjectResult(new { balance = balance });
        }

        [HttpPost("send", Name = "WalletTransfer")]
        public async Task<IActionResult> WalletTransfer([FromBody] SendPaymentDto sendPaymentDto)
        {
            double balance = 0d;

            var session = new Session(sendPaymentDto.Credentials.Identifier.ToSecureString(), sendPaymentDto.Credentials.Password.ToSecureString())
            {
                Amount = sendPaymentDto.Amount.ConvertToUInt64(),
                ForwardMessage = sendPaymentDto.CreateRedemptionKey,
                Memo = sendPaymentDto.Memo,
                RecipientAddress = sendPaymentDto.Address
            };

            try
            {
                await actorService.Tansfer(session);

                if (actorService.State != State.Committed)
                {
                    session = actorService.GetSession(session.SessionId);
                    var failedMessage = JsonConvert.SerializeObject(session.LastError.GetValue("message"));
                    return new ObjectResult(new { error = failedMessage, statusCode = 500 });
                }

                session = actorService.GetSession(session.SessionId);

                var messageStore = await unitOfWork
                                            .GetRedemptionRepository()
                                            .Get(session.Identifier, session.MasterKey, StoreKey.TransactionIdKey, session.SessionId.ToString());

                balance = await walletService.AvailableBalance(session.Identifier, session.MasterKey);

                if (sendPaymentDto.CreateRedemptionKey)
                    return new OkObjectResult(new { message = messageStore.Message });
            }
            catch (Exception ex)
            {
                balance = await walletService.AvailableBalance(session.Identifier, session.MasterKey);
                return new ObjectResult(new { error = ex.Message, statusCode = 500, balance = balance });
            }

            return new OkObjectResult(new { balance = balance });
        }

        [HttpPost("transactions", Name = "WalletTransactions")]
        public async Task<IActionResult> WalletTransactions([FromBody] CredentialsDto credentials)
        {
            var txns = await unitOfWork.GetTransactionRepository().All(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            return new OkObjectResult(txns);
        }

        [HttpPost("vaultunseal", Name = "VaultUnseal")]
        public async Task<IActionResult> VaultUnseal([FromBody] ShardDto key)
        {
            var success = await vaultServiceClient.Unseal(key.Shard.ToSecureString());
            return new OkObjectResult(new { success = success });
        }
    }
}
