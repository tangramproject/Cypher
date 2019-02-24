using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Vault;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using Newtonsoft.Json;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "transfer" }, "Transfer funds")]
    public class WalletTransferCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        readonly IVaultService vaultService;

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
                var identifier = Prompt.GetPassword("Identifier:", ConsoleColor.Yellow).ToSecureString();
                var password = Prompt.GetPassword("Password:", ConsoleColor.Yellow).ToSecureString();
                var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
                var address = Prompt.GetString("To:", null, ConsoleColor.Red);
                var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
                var yesNo = Prompt.GetYesNo("Send redemption key to message pool?", true, ConsoleColor.Yellow);

                using (var insecureIdentifier = identifier.Insecure())
                using (var insecurePassword = password.Insecure())
                {
                    await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");
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

                    console.WriteLine(JsonConvert.SerializeObject(message));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
