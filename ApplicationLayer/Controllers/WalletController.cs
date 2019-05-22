// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helper;

namespace TangramCypher.ApplicationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController
    {
        private readonly IActorService actorService;
        private readonly IWalletService walletService;
        private readonly IVaultServiceClient vaultServiceClient;

        public WalletController(IActorService actorService, IWalletService walletService, IVaultServiceClient vaultServiceClient)
        {
            this.actorService = actorService;
            this.walletService = walletService;
            this.vaultServiceClient = vaultServiceClient;
        }

        [HttpPost("address", Name = "CreateWalletAddress")]
        public async Task<IActionResult> CreateWalletAddress([FromBody] CredentialsDto credentials)
        {
            var pksk = walletService.CreatePkSk();
            var added = await walletService.AddKey(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString(), pksk);

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
            return new OkObjectResult(profile);
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
                await actorService
                      .MasterKey(receivePaymentDto.Credentials.Password.ToSecureString())
                      .Identifier(receivePaymentDto.Credentials.Identifier.ToSecureString())
                      .FromAddress(receivePaymentDto.FromAddress)
                      .ReceivePayment();

                balance = await actorService.CheckBalance();
            }
            catch (Exception ex)
            {
                balance = await actorService.CheckBalance();
                return new ObjectResult(new { error = ex.Message, statusCode = 500, balance });
            }

            return new OkObjectResult(new { balance });
        }

        [HttpPost("send", Name = "WalletTransfer")]
        public async Task<IActionResult> WalletTransfer([FromBody] SendPaymentDto sendPaymentDto)
        {
            double balance = 0d;

            try
            {
                var sent = await actorService
                                 .MasterKey(sendPaymentDto.Credentials.Password.ToSecureString())
                                 .Identifier(sendPaymentDto.Credentials.Identifier.ToSecureString())
                                 .Amount(sendPaymentDto.Amount)
                                 .ToAddress(sendPaymentDto.ToAddress)
                                 .Memo(sendPaymentDto.Memo)
                                 .SendPayment();

                if (sent.Equals(false))
                {
                    var failedMessage = JsonConvert.SerializeObject(actorService.GetLastError().GetValue("message"));
                    return new ObjectResult(new { error = failedMessage, statusCode = 500 });
                }

                var networkMessage = await actorService.SendPaymentMessage(true);
                var success = networkMessage.GetValue("success").ToObject<bool>();

                if (success.Equals(false))
                    return new ObjectResult(new { error = JsonConvert.SerializeObject(networkMessage.GetValue("message")), statusCode = 500 });

                balance = await actorService.CheckBalance();
            }
            catch (Exception ex)
            {
                balance = await actorService.CheckBalance();
                return new ObjectResult(new { error = ex.Message, statusCode = 500, balance });
            }

            return new OkObjectResult(new { balance });
        }

        [HttpPost("transactions", Name = "WalletTransactions")]
        public async Task<IActionResult> WalletTransactions([FromBody] CredentialsDto credentials)
        {
            var creds = await walletService.Transactions(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
            return new OkObjectResult(creds);
        }

        [HttpPost("vaultunseal", Name = "VaultUnseal")]
        public async Task<IActionResult> VaultUnseal([FromBody] ShardDto key)
        {
            await vaultServiceClient.Unseal(key.Shard.ToSecureString());
            return new OkObjectResult(true);
        }
    }
}
