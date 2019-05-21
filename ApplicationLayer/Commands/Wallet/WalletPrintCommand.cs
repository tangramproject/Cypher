// Cypher (c) by Tangram Inc
//
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Vault;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.ApplicationLayer.Coin;
using Kurukuru;
using System.Security;
using Microsoft.Extensions.Logging;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "print" }, "Print money")]
    public class WalletPrintCommand : Command
    {
        readonly IActorService actorService;
        readonly IWalletService walletService;
        readonly ICoinService coinService;
        readonly IConsole console;
        readonly ILogger logger;

        private Spinner spinner;

        public WalletPrintCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            walletService = serviceProvider.GetService<IWalletService>();
            coinService = serviceProvider.GetService<ICoinService>();
            console = serviceProvider.GetService<IConsole>();
            logger = serviceProvider.GetService<ILogger>();

            actorService.MessagePump += ActorService_MessagePump;
        }

        private (RedemptionKeyDto, CoinDto) PrintTheMoney(double amount) {
            var txCoin = new TransactionCoin();
            // Right now, only the Input field is used for BuildReceiver
            txCoin.Input = amount;
            var coin = coinService
                .TransactionCoin(txCoin)
                .BuildReceiver()
                .Coin();
            var (key1, key2) = coinService.HotRelease(coin.Version, coin.Stamp, coinService.Password());
            txCoin = coinService.TransactionCoin();
            var redemption = new RedemptionKeyDto
            {
                Amount = txCoin.Input,
                Blind = txCoin.Blind,
                Hash = coin.Hash,
                Key1 = key1,
                Key2 = key2,
                Memo = "Completely legitimate money that was not printed! Guaranteed or your money back (literally).",
                Stamp = coin.Stamp
            };

            return (redemption, coin);
        }

        public override async Task Execute()
        {

            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
            {
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
                            coinService.Password("printer".ToSecureString());
                            var (redemption, coin) = PrintTheMoney(amount);

                            spinner.Text = "Transferring money";
                            actorService
                                  .MasterKey(password)
                                  .Identifier(identifier)
                                  .FromAddress(address);
                            await actorService.Payment(redemption, coin);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
                            logger.LogError(ex.StackTrace);
                            amount = 0;
                            throw ex;
                        }
                        finally
                        {
                            var balance = Convert.ToString(await actorService.CheckBalance());

                            spinner.Text = $"Printed: {amount}  Available Balance: {balance}";
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

