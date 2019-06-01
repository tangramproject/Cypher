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
using System.IO;
using Kurukuru;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Wallet;
using System.Security;
using Microsoft.Extensions.Logging;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "send" }, "Send funds")]
    public class WalletTransferCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        readonly ILogger logger;

        private Spinner spinner;

        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        public WalletTransferCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
            logger = serviceProvider.GetService<ILogger>();

            actorService.MessagePump += ActorService_MessagePump;
        }

        public override async Task Execute()
        {

            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
            {
                var address = Prompt.GetString("To:", null, ConsoleColor.Red);
                var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
                var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
                var yesNo = Prompt.GetYesNo("Send redemption key to message pool?", true, ConsoleColor.Yellow);


                if (double.TryParse(amount, out double t))
                {
                    await Spinner.StartAsync("Processing payment ...", async spinner =>
                    {
                        this.spinner = spinner;

                        try
                        {
                            var sent = await actorService
                                      .MasterKey(password)
                                      .Identifier(identifier)
                                      .Amount(t)
                                      .ToAddress(address)
                                      .Memo(memo)
                                      .SendPayment();

                            if (sent.Equals(false))
                            {
                                var failedMessage = JsonConvert.SerializeObject(actorService.GetLastError().GetValue("message"));
                                logger.LogCritical(failedMessage);
                                spinner.Fail(failedMessage);
                                return;
                            }

                            switch (yesNo)
                            {
                                case true:
                                    var networkMessage = await actorService.SendPaymentMessage(true);
                                    var success = networkMessage.GetValue("success").ToObject<bool>();

                                    if (success.Equals(false))
                                        spinner.Fail(JsonConvert.SerializeObject(networkMessage.GetValue("message")));

                                    break;

                                case false:
                                    var localMessage = await actorService.SendPaymentMessage(false);

                                    spinner.Stop();

                                    SaveRedemptionKeyLocal(localMessage);

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.StackTrace);
                            throw ex;
                        }
                        finally
                        {
                            spinner.Text = $"Available Balance: {Convert.ToString(await actorService.CheckBalance())}";
                        }
                    });
                }
            }
        }

        private void SaveRedemptionKeyLocal(JObject message)
        {
            var msg = message.GetValue("message").ToObject<MessageDto>();

            console.ForegroundColor = ConsoleColor.Magenta;
            console.WriteLine("\nOptions:");
            console.WriteLine("Save redemption key to file [1]");
            console.WriteLine("Copy redemption key from console [2]\n");

            var option = Prompt.GetInt("Select option:", 1, ConsoleColor.Yellow);

            console.ForegroundColor = ConsoleColor.White;

            var content =
                "--------------Begin Redemption Key--------------" +
                Environment.NewLine +
                JsonConvert.SerializeObject(msg) +
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
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}
