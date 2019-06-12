// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Vault;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.ApplicationLayer.Wallet;
using Kurukuru;
using System.Security;
using Microsoft.Extensions.Logging;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "receive" }, "Receive payment")]
    public class WalletReceivePaymentCommand : Command
    {
        readonly IActorService actorService;
        readonly IWalletService walletService;
        readonly IConsole console;
        readonly ILogger logger;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            walletService = serviceProvider.GetService<IWalletService>();
            console = serviceProvider.GetService<IConsole>();
            logger = serviceProvider.GetService<ILogger>();

            actorService.MessagePump += ActorService_MessagePump;
        }

        public override async Task Execute()
        {

            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
            {
                var address = Prompt.GetString("Address:", null, ConsoleColor.Red);

                if (!string.IsNullOrEmpty(address))
                {
                    await Spinner.StartAsync("Processing receive payment(s) ...", async spinner =>
                    {
                        this.spinner = spinner;

                        await Task.Delay(500);

                        try
                        {
                            var session = new Session(identifier, password) { SenderAddress = address };
                            await actorService.ReceivePayment(session);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                            throw ex;
                        }
                        finally
                        {
                            var transaction = await walletService.LastTransaction(identifier, password, TransactionType.Receive);
                            var txnAmount = transaction == null ? 0 : transaction.Amount;
                            var txnMemo = transaction == null ? "" : transaction.Memo;
                            var balance = await walletService.AvailableBalance(identifier, password);

                            spinner.Text = $"Memo:{txnMemo}  Received:{txnAmount}  Available Balance: {balance.Result.DivWithNaT().ToString("F9")}";
                        }
                    }, Patterns.Toggle);
                }
            }
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}

