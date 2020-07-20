// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Tangram.Core.Actor;
using Tangram.Core.Helper;
using Tangram.Core.Wallet;
using Kurukuru;
using Microsoft.Extensions.Logging;
using Tangram.Core.Model;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "receive" }, "Receive payment")]
    public class WalletReceivePaymentCommand : Command
    {
        private readonly IActorService _actorService;
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
        {
            _actorService = serviceProvider.GetService<IActorService>();
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger>();

            _actorService.MessagePump += ActorService_MessagePump;
        }

        public override async Task Execute()
        {

            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var address = Prompt.GetString("Address:", null, ConsoleColor.Red);

            if (!string.IsNullOrEmpty(address))
            {
                await Spinner.StartAsync("Processing receive payment(s) ...", async spinner =>
                {
                    this.spinner = spinner;

                    await Task.Delay(500);

                    try
                    {
                        var session = new Session(identifier, passphrase) { SenderAddress = address };
                        await _actorService.ReceivePayment(session);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        throw ex;
                    }
                    finally
                    {
                        var transaction = _walletService.LastTransaction(identifier, passphrase, TransactionType.Receive);
                        var txnReceivedAmount = transaction == null ? 0.ToString() : transaction.Balance.DivWithNaT().ToString("F9");
                        var txnMemo = transaction == null ? "" : transaction.Memo;
                        var balance = _walletService.AvailableBalance(identifier, passphrase);

                        spinner.Text = $"Memo: {txnMemo}  Received: {txnReceivedAmount}  Available Balance: {balance.Result.DivWithNaT():F9}";
                    }
                }, Patterns.Toggle3);
            }
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}

