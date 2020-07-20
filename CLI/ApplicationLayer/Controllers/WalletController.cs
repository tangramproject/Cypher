// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Tangram.Core.Actor;
using Tangram.Core.Helper;
using Tangram.Core.Model;
using Tangram.Core.Send;
using Tangram.Core.Wallet;

namespace Tangram.Bamboo.ApplicationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController
    {
        private readonly IActorService actorService;
        private readonly IWalletService walletService;
        private readonly ISendService sendService;


        public WalletController(IActorService actorService, IWalletService walletService, ISendService sendService)
        {
            this.actorService = actorService;
            this.walletService = walletService;
            this.sendService = sendService;
        }

        //[HttpPost("address", Name = "CreateWalletAddress")]
        //public IActionResult CreateWalletAddress([FromBody] Credentials credentials)
        //{
        //    var session = new Session(credentials.Identifier.ToSecureString(), credentials.Password.ToSecureString());
        //    var keySet = walletService.CreateKeySet();

        //    using var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString());

        //    try
        //    {
        //        db.Insert(keySet);
        //        return new CreatedResult("httpWallet", new { success = true });
        //    }
        //    catch (Exception)
        //    {
        //        return new BadRequestResult();
        //    }
        //}

        //[HttpPost("balance", Name = "WalletBalance")]
        //public IActionResult WalletBalance([FromBody] Credentials credentials)
        //{
        //    var total = walletService.AvailableBalance(credentials.Identifier.ToSecureString(), credentials.Mnemonic.ToSecureString());
        //    return new OkObjectResult(new { balance = total });
        //}

        //[HttpGet("create", Name = "CreateWallet")]
        //public IActionResult CreateWallet()
        //{
        //    var creds = walletService.CreateWallet();
        //    return new OkObjectResult(creds);
        //}

        //[HttpPost("profile", Name = "WalletProfile")]
        //public IActionResult WalletProfile([FromBody] Credentials credentials)
        //{
        //    var profile = walletService.ListKeySets(credentials.Passphrase.ToSecureString(), credentials.Identifier);
        //    return new OkObjectResult(profile);
        //}

        //[HttpGet("list", Name = "WalletList")]
        //public IActionResult WalletList()
        //{
        //    var list = walletService.WalletList();
        //    return new OkObjectResult(list);
        //}

        //[HttpPost("receive", Name = "WalletReceivePayment")]
        //public async Task<IActionResult> WalletReceivePayment([FromBody] ReceivePayment receivePayment)
        //{
        //    TaskResult<ulong> availBalance;

        //    var session = new Session(receivePayment.Credentials.Identifier.ToSecureString(), receivePayment.Credentials.Passphrase.ToSecureString())
        //    {
        //        SenderAddress = receivePayment.FromAddress
        //    };

        //    try
        //    {
        //        if (receivePayment.RedemptionMessage != null)
        //        {
        //            await actorService
        //                    .ReceivePaymentRedemptionKey(session, JsonConvert.SerializeObject(receivePayment.RedemptionMessage));
        //        }
        //        else
        //        {
        //            await actorService.ReceivePayment(session);
        //        }

        //        availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);
        //    }
        //    catch (Exception ex)
        //    {
        //        availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);
        //        return new ObjectResult(new { error = ex.Message, statusCode = 500, balance = availBalance.Result });
        //    }

        //    return new OkObjectResult(new { balance = availBalance.Result });
        //}

        //[HttpPost("send", Name = "WalletTransfer")]
        //public async Task<IActionResult> WalletTransfer([FromBody] SendPayment sendPayment)
        //{
        //    TaskResult<ulong> availBalance;

        //    var session = new Session(sendPayment.Credentials.Identifier.ToSecureString(), sendPayment.Credentials.Passphrase.ToSecureString())
        //    {
        //        Amount = sendPayment.Amount.ConvertToUInt64(),
        //        ForwardMessage = sendPayment.CreateRedemptionKey,
        //        Memo = sendPayment.Memo,
        //        RecipientAddress = sendPayment.Address
        //    };

        //    try
        //    {
        //        await sendService.Tansfer(session);

        //        if (sendService.State != State.Committed)
        //        {
        //            session = actorService.GetSession(session.SessionId);
        //            var failedMessage = JsonConvert.SerializeObject(session.LastError.GetValue("message"));
        //            return new ObjectResult(new { error = failedMessage, statusCode = 500 });
        //        }

        //        session = actorService.GetSession(session.SessionId);

        //        using var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString());
        //        var messageStore = db.Query<MessageStore>().Where(m => m.Equals(session.SessionId)).FirstOrDefault();

        //        availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);

        //        if (sendPayment.CreateRedemptionKey)
        //            return new OkObjectResult(new { message = messageStore.Message });

        //    }
        //    catch (Exception ex)
        //    {
        //        availBalance = walletService.AvailableBalance(session.Identifier, session.MasterKey);
        //        return new ObjectResult(new { error = ex.Message, statusCode = 500, balance = availBalance.Result });
        //    }

        //    return new OkObjectResult(new { balance = availBalance.Result });
        //}

        //[HttpPost("transactions", Name = "WalletTransactions")]
        //public IActionResult WalletTransactions([FromBody] Credentials credentials)
        //{
        //    var session = new Session(credentials.Identifier.ToSecureString(), credentials.Passphrase.ToSecureString());
        //    using var db = Util.LiteRepositoryFactory(session.MasterKey, session.Identifier.ToUnSecureString());
        //    var txns = db.Query<Transaction>();

        //    return new OkObjectResult(txns);
        //}
    }
}
