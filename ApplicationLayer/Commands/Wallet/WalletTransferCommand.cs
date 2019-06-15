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
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "send" }, "Send funds")]
    public class WalletTransferCommand : Command
    {
        readonly IActorService actorService;
        readonly IWalletService walletService;
        readonly IUnitOfWork unitOfWork;
        readonly IConsole console;
        readonly ILogger logger;

        private Spinner spinner;

        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        public WalletTransferCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            walletService = serviceProvider.GetService<IWalletService>();
            unitOfWork = serviceProvider.GetService<IUnitOfWork>();
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
                            var session = new Session(identifier, password)
                            {
                                Amount = t.ConvertToUInt64(),
                                ForwardMessage = yesNo,
                                Memo = memo,
                                RecipientAddress = address
                            };

                            await actorService.Tansfer(session);
                            session = actorService.GetSession(session.SessionId);

                            if (actorService.State != State.Completed)
                            {
                                var failedMessage = JsonConvert.SerializeObject(session.LastError.GetValue("message"));
                                logger.LogCritical(failedMessage);
                                spinner.Fail(failedMessage);
                                return;
                            }

                            if (session.ForwardMessage.Equals(false))
                            {
                                await SaveRedemptionKeyLocal(session.SessionId);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.StackTrace);
                            throw ex;
                        }
                        finally
                        {
                            var balance = await walletService.AvailableBalance(identifier, password);
                            spinner.Text = $"Available Balance: {balance.Result.DivWithNaT().ToString("F9")}";
                        }
                    }, Patterns.Toggle3);
                }
            }
        }

        private async Task SaveRedemptionKeyLocal(Guid sessionId)
        {
            spinner.Text = string.Empty;
            spinner.Stop();

            var session = actorService.GetSession(sessionId);
            var getMessageStore = await unitOfWork
                        .GetRedemptionRepository()
                        .Get(session, StoreKey.TransactionIdKey, session.SessionId.ToString());

            if (getMessageStore.Success.Equals(false))
            {
                console.WriteLine($"Error: {getMessageStore.Exception.Message}");
                return;
            }

            console.ForegroundColor = ConsoleColor.Magenta;
            console.WriteLine("\nOptions:");
            console.WriteLine("Save redemption key to file [1]");
            console.WriteLine("Copy redemption key from console [2]\n");

            var option = Prompt.GetInt("Select option:", 1, ConsoleColor.Yellow);

            console.ForegroundColor = ConsoleColor.White;

            try
            {
                var content =
                    "--------------Begin Redemption Key--------------" +
                    Environment.NewLine +
                    JsonConvert.SerializeObject(getMessageStore.Result.Message) +
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
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                throw ex;
            }

        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}
