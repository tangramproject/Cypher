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
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.ApplicationLayer.Coin;
using Kurukuru;
using Microsoft.Extensions.Logging;
using TangramCypher.Model;
using TangramCypher.ApplicationLayer.Send;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    public class WalletPrintCommand : Command
    {
        readonly IActorService actorService;
        readonly IWalletService walletService;
        readonly ICoinService coinService;
        readonly IConsole console;
        readonly ILogger logger;
        readonly ISendService sendService;
        private Spinner spinner;

        public WalletPrintCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            walletService = serviceProvider.GetService<IWalletService>();
            coinService = serviceProvider.GetService<ICoinService>();
            console = serviceProvider.GetService<IConsole>();
            logger = serviceProvider.GetService<ILogger>();
            sendService = serviceProvider.GetService<ISendService>();

            actorService.MessagePump += ActorService_MessagePump;
        }

        public override async Task Execute()
        {

            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
            {
                var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
                var address = Prompt.GetString("Address:", null, ConsoleColor.Red);
                var amountStr = Prompt.GetString("Amount:", null, ConsoleColor.Red);

                if (!string.IsNullOrEmpty(address) && double.TryParse(amountStr, out double amount))
                {
                    await Spinner.StartAsync("Revving up the printer", async spinner =>
                    {
                        this.spinner = spinner;

                        await Task.Delay(500);

                        try
                        {
                            spinner.Text = "Transferring money";

                            var session = new Session(identifier, password)
                            {
                                Amount = amount.ConvertToUInt64(),
                                ForwardMessage = true,
                                Memo = memo,
                                RecipientAddress = address
                            };

                            await sendService.Tansfer(session);

                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
                            logger.LogError(ex.StackTrace);
                            throw ex;
                        }
                        finally
                        {
                            spinner.Text = "Printed...";
                        }
                    });
                }
            }
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}

