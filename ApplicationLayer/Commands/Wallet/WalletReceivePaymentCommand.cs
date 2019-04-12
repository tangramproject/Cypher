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

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "receive" }, "Receive payment")]
    public class WalletReceivePaymentCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        readonly IVaultService vaultService;
        readonly IWalletService walletService;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
            vaultService = serviceProvider.GetService<IVaultService>();
            walletService = serviceProvider.GetService<IWalletService>();

            actorService.MessagePump += ActorService_MessagePump;
        }         

        public override async Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
                {
                    var address = Prompt.GetString("Address:", null, ConsoleColor.Red);

                    using (var insecureIdentifier = identifier.Insecure())
                    using (var insecurePassword = password.Insecure())
                    {
                        await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");
                    }

                    if (!string.IsNullOrEmpty(address))
                    {
                        await Spinner.StartAsync("Processing receive payment ...", async spinner =>
                        {
                            this.spinner = spinner;

                            await actorService
                                      .From(password)
                                      .Identifier(identifier)
                                      .ReceivePayment(address);

                            spinner.Text = "Fetching balance ...";

                            await Task.Delay(1500);
                            await CheckBalance(identifier, password);

                            spinner.Text = "Done ...";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task CheckBalance(SecureString identifier, SecureString password)
        {
            var total = await walletService.AvailableBalance(identifier, password);

            console.ForegroundColor = ConsoleColor.Magenta;
            console.WriteLine($"\nAvailable Balance: {total}\n");
            console.ForegroundColor = ConsoleColor.White;
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}
