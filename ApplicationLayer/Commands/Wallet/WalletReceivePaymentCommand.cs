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
using TangramCypher.Helper.LibSodium;
using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "receive" }, "Receive payment")]
    public class WalletReceivePaymentCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        readonly IVaultService vaultService;
        readonly IWalletService walletService;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
            vaultService = serviceProvider.GetService<IVaultService>();
            walletService = serviceProvider.GetService<IWalletService>();
        }

        public override async Task Execute()
        {
            try
            {
                var identifier = Prompt.GetPassword("Identifier:", ConsoleColor.Yellow).ToSecureString();
                var password = Prompt.GetPassword("Password:", ConsoleColor.Yellow).ToSecureString();
                var address = Prompt.GetString("Address:", null, ConsoleColor.Red);

                using (var insecureIdentifier = identifier.Insecure())
                using (var insecurePassword = password.Insecure())
                {
                    await vaultService.GetDataAsync(insecureIdentifier.Value, insecurePassword.Value, $"wallets/{insecureIdentifier.Value}/wallet");
                }

                if (!string.IsNullOrEmpty(address))
                {
                    await actorService
                      .From(password)
                      .Identifier(identifier)
                      .ReceivePayment(address);

                    var total = await walletService.AvailableBalance(identifier, password);

                    console.ForegroundColor = ConsoleColor.Magenta;
                    console.WriteLine($"\nWallet balance: {total}\n");
                    console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
