using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helpers;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using TangramCypher.ApplicationLayer.Wallet;
using System.Security;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "reward" }, "Reward wallet")]
    public class WalletRewardCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        readonly IWalletService walletService;

        public WalletRewardCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
            walletService = serviceProvider.GetService<IWalletService>();
        }

        public override async Task Execute()
        {
            var identifier = Prompt.GetPassword("Identifier:", ConsoleColor.Yellow).ToSecureString();
            var password = Prompt.GetPassword("Password:", ConsoleColor.Yellow).ToSecureString();
            var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);

            try
            {
                var coin = await AddCoin(password, Convert.ToDouble(amount));

                await walletService.AddEnvelope(identifier, password, coin.Envelope);

                console.WriteLine($"Wallet updated with {amount}");
            }
            catch (NullReferenceException ex)
            {
                console.ForegroundColor = ConsoleColor.Red;
                console.WriteLine($"Wallet failed to updated.\n Error: {ex.Message}");
                console.ForegroundColor = ConsoleColor.White;
            }
            catch (ApiException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        async Task<CoinDto> AddCoin(SecureString passphrase, double? amount)
        {
            var coin = actorService.DeriveCoin(passphrase, 1, actorService.DeriveEnvelope(passphrase, 1, amount.Value));

            coin = actorService.FormatCoinToBase64(coin);

            var result = await actorService.AddCoinAsync(coin, new System.Threading.CancellationToken());

            if (result != null)
                return coin;

            return null;
        }
    }
}
