// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Wallet;
using Microsoft.Extensions.DependencyInjection;
using ConsoleTables;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "transactions" }, "List wallet transactions")]
    public class WalletTxHistoryCommand : Command
    {
        private readonly IConsole console;
        private readonly IWalletService walletService;

        public WalletTxHistoryCommand(IServiceProvider serviceProvider)
        {
            console = serviceProvider.GetService<IConsole>();
            walletService = serviceProvider.GetService<IWalletService>();
        }

        public override Task Execute()
        {
            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
            {
                try
                {
                    var final = walletService.TransactionHistory(identifier, password).ToList();

                    if (final?.Any() == true)
                    {
                        var table = ConsoleTable.From(final).ToString();
                        console.WriteLine(table);
                        return Task.CompletedTask;
                    }

                    NoTxn();
                }
                catch (Exception)
                {
                    NoTxn();
                }
            }

            return Task.CompletedTask;
        }

        private void NoTxn()
        {
            console.ForegroundColor = ConsoleColor.Red;
            console.WriteLine($"\nWallet has no transactions.\n");
            console.ForegroundColor = ConsoleColor.White;
        }
    }
}
