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
                var token = await AddToken(password, Convert.ToDouble(amount));

                await walletService.AddEnvelope(identifier, password, token.Envelope);

                console.WriteLine($"Wallet updated with {amount}");
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

        async Task<TokenDto> AddToken(SecureString passphrase, double? amount)
        {
            var token = actorService.DeriveToken(passphrase, 1, actorService.DeriveEnvelope(passphrase, 1, amount.Value));

            token.Envelope.Serial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Envelope.Serial));
            token.Hint = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Hint));
            token.Keeper = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Keeper));
            token.Principle = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Principle));
            token.Stamp = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Stamp));

            var result = await actorService.AddTokenAsync(token, new System.Threading.CancellationToken());
            var transaction = JsonConvert.DeserializeObject<JObject>(result.AsJson().ReadAsStringAsync().Result);

            if (transaction != null)
                return token;

            return null;
        }
    }
}
