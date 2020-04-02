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
using TangramCypher.ApplicationLayer.Send;
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
        private readonly ISendService sendService;


        public WalletController(IActorService actorService, IWalletService walletService, IVaultServiceClient vaultServiceClient, ISendService sendService)
        {
            this.actorService = actorService;
            this.walletService = walletService;
            this.vaultServiceClient = vaultServiceClient;
            this.sendService = sendService;
        }

        [HttpPost("address", Name = "CreateWalletAddress")]
        public IActionResult CreateWalletAddress([FromBody] CredentialsDto credentials)
        {
            var session = new Session(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            var keySet = walletService.CreateKeySet();

            using var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString());

            try
            {
                db.Insert(keySet);
                return new CreatedResult("httpWallet", new { success = true });
            }
            catch (Exception)
            {
                return new BadRequestResult();
            }
        }

        [HttpPost("balance", Name = "WalletBalance")]
        public IActionResult WalletBalance([FromBody] CredentialsDto credentials)
        {
            var total = walletService.AvailableBalance(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            return new OkObjectResult(new { balance = total });
        }

        [HttpGet("create", Name = "CreateWallet")]
        public IActionResult CreateWallet()
        {
            var creds = walletService.CreateWallet();
            return new OkObjectResult(creds);
        }

        [HttpPost("profile", Name = "WalletProfile")]
        public IActionResult WalletProfile([FromBody] CredentialsDto credentials)
        {
            var profile = walletService.ListKeySets(credentials.Password.ToSecureString(), credentials.Identifier);
            return new OkObjectResult(profile);
        }

        [HttpGet("list", Name = "WalletList")]
        public IActionResult WalletList()
        {
            var list = walletService.WalletList();
            return new OkObjectResult(list);
        }

        [HttpPost("receive", Name = "WalletReceivePayment")]
        public async Task<IActionResult> WalletReceivePayment([FromBody] ReceivePaymentDto receivePaymentDto)
        {
            TaskResult<ulong> availBalance;

            var session = new Session(receivePaymentDto.Credentials.Identifier.ToSecureString(), receivePaymentDto.Credentials.Password.ToSecureString())
            {
                SenderAddress = receivePaymentDto.FromAddress
            };

            try
            {
                if (receivePaymentDto.RedemptionMessage != null)
                {
                    await actorService
                            .ReceivePaymentRedemptionKey(session, JsonConvert.SerializeObject(receivePaymentDto.RedemptionMessage));
                }
                else
                {
                    await actorService.ReceivePayment(session);
                }

                availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);
            }
            catch (Exception ex)
            {
                availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);
                return new ObjectResult(new { error = ex.Message, statusCode = 500, balance = availBalance.Result });
            }

            return new OkObjectResult(new { balance = availBalance.Result });
        }

        [HttpPost("send", Name = "WalletTransfer")]
        public async Task<IActionResult> WalletTransfer([FromBody] SendPaymentDto sendPaymentDto)
        {
            TaskResult<ulong> availBalance;

            var session = new Session(sendPaymentDto.Credentials.Identifier.ToSecureString(), sendPaymentDto.Credentials.Password.ToSecureString())
            {
                Amount = sendPaymentDto.Amount.ConvertToUInt64(),
                ForwardMessage = sendPaymentDto.CreateRedemptionKey,
                Memo = sendPaymentDto.Memo,
                RecipientAddress = sendPaymentDto.Address
            };

            try
            {
                await sendService.Tansfer(session);

                if (sendService.State != State.Committed)
                {
                    session = actorService.GetSession(session.SessionId);
                    var failedMessage = JsonConvert.SerializeObject(session.LastError.GetValue("message"));
                    return new ObjectResult(new { error = failedMessage, statusCode = 500 });
                }

                session = actorService.GetSession(session.SessionId);

                using var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString());
                var messageStore = db.Query<MessageStoreDto>().Where(m => m.Equals(session.SessionId)).FirstOrDefault();

                availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);

                if (sendPaymentDto.CreateRedemptionKey)
                    return new OkObjectResult(new { message = messageStore.Message });

            }
            catch (Exception ex)
            {
                availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);
                return new ObjectResult(new { error = ex.Message, statusCode = 500, balance = availBalance.Result });
            }

            return new OkObjectResult(new { balance = availBalance.Result });
        }

        [HttpPost("transactions", Name = "WalletTransactions")]
        public IActionResult WalletTransactions([FromBody] CredentialsDto credentials)
        {
            var session = new Session(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            using var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString());
            var txns = db.Query<TransactionDto>();

            return new OkObjectResult(txns);
        }

        [HttpPost("vaultunseal", Name = "VaultUnseal")]
        public async Task<IActionResult> VaultUnseal([FromBody] ShardDto key)
        {
            var success = await vaultServiceClient.Unseal(key.Shard.ToSecureString());
            return new OkObjectResult(new { success });
        }
    }
}
