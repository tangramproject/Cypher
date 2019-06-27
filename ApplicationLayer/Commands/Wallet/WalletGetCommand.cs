// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.Helper;
using TangramCypher.ApplicationLayer.Wallet;
using ConsoleTables;
using System.Linq;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "get" }, "Retrieves the contents of a wallet")]
    class WalletGetCommand : Command
    {
        private readonly IWalletService walletService;
        private readonly IConsole console;

        public WalletGetCommand(IServiceProvider serviceProvider)
        {
            walletService = serviceProvider.GetService<IWalletService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
            {
                using (var id = identifier.Insecure())
                {
                    var addresses = walletService.ListAddresses(password, identifier.ToUnSecureString());

                    if (addresses?.Any() == true)
                    {
                        var table = new ConsoleTable("Address");

                        foreach (var address in addresses)
                            table.AddRow(address);

                        console.WriteLine(table);
                    }
                    else
                    {
                        console.ForegroundColor = ConsoleColor.Red;
                        console.WriteLine("No addresses have been created.");
                        console.ForegroundColor = ConsoleColor.White;
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
}
