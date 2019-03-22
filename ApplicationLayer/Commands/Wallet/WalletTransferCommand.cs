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
using Newtonsoft.Json;
using System.Text;
using System.IO;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "transfer" }, "Transfer funds")]
    public class WalletTransferCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        readonly IVaultService vaultService;

        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        public WalletTransferCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
            vaultService = serviceProvider.GetService<IVaultService>();
        }
        public override async Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
                {
                    var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
                    var address = Prompt.GetString("To:", null, ConsoleColor.Red);
                    var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
                    var yesNo = Prompt.GetYesNo("Send redemption key to message pool?", true, ConsoleColor.Yellow);

                    using (var insecureIdentifier = identifier.Insecure())
                    {
                        await vaultService.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");
                    }

                    if (double.TryParse(amount, out double t))
                    {
                        var message =
                            await actorService
                                    .From(password)
                                    .Identifier(identifier)
                                    .Amount(t)
                                    .To(address)
                                    .Memo(memo)
                                    .SendPayment(yesNo);

                        if (yesNo.Equals(false))
                        {
                            var notification = message.ToObject<NotificationDto>();

                            console.ForegroundColor = ConsoleColor.Magenta;
                            console.WriteLine("\nOptions:");
                            console.WriteLine("Save redemption key to file [1]");
                            console.WriteLine("Copy redemption key from console [2]\n");

                            var option = Prompt.GetInt("Select option:", 1, ConsoleColor.Yellow);

                            console.ForegroundColor = ConsoleColor.White;

                            var content =
                                "--------------Begin Redemption Key--------------" +
                                Environment.NewLine +
                                JsonConvert.SerializeObject(notification) +
                                Environment.NewLine +
                                "--------------End Redemption Key----------------";

                            if (option.Equals(1))
                            {
                                var path = $"{tangramDirectory}redem{DateTime.Now.GetHashCode()}.rdkey";
                                File.WriteAllText(path, content);
                                console.WriteLine($"\nSaved path: {path}\n");
                            }
                            else
                                console.WriteLine($"\n{content}\n");

                            return;
                        }

                        console.WriteLine(JsonConvert.SerializeObject(message));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
