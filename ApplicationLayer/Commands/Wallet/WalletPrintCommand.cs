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
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
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

                            actorService
                                  .MasterKey(password)
                                  .Identifier(identifier)
                                  .ToAddress(address)
                                  .Memo(memo);

                            await actorService.SetRandomAddress();
                            await actorService.SetSecretKey();
                            await actorService.SetPublicKey();

                            var coin = coinService
                                .TransactionCoin(new TransactionCoinDto { Input = (ulong)amount })
                                .BuildReceiver(password)
                                .Coin();

                            coin.Hash = coinService.Hash(coin).ToHex();
                            coin.Network = walletService.NetworkAddress(coin).ToHex();

                            var c = SendCoin(coin);

                            if (c == null)
                                spinner.Fail("Something went wrong ;(");

                            var networkMessage = await actorService.SendPaymentMessage(true);
                            var success = networkMessage.GetValue("success").ToObject<bool>();

                            if (success.Equals(false))
                                spinner.Fail(JsonConvert.SerializeObject(networkMessage.GetValue("message")));

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

        private CoinDto SendCoin(CoinDto coin)
        {
            spinner.Text = "Sending printed coin ;)";

            var coinResult = actorService.AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin).GetAwaiter().GetResult();
            if (coinResult == null)
            {
                for (int i = 0; i < 10; i++)
                {
                    spinner.Text = $"Retrying sending coin {i} of 10";
                    coinResult = actorService.AddAsync(coin.FormatCoinToBase64(), RestApiMethod.PostCoin).GetAwaiter().GetResult();

                    Task.Delay(100).Wait();

                    if (coinResult != null)
                        break;
                }
            }

            return coinResult;
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}

