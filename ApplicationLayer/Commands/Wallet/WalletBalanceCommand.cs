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
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helper;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "balance" }, "Get current wallet balance")]
    public class WalletBalanceCommand : Command
    {
        readonly IWalletService walletService;
        readonly IConsole console;

        public WalletBalanceCommand(IServiceProvider serviceProvider)
        {
            walletService = serviceProvider.GetService<IWalletService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
                {
                    var total = await walletService.AvailableBalanceGeneric(identifier, password);

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
