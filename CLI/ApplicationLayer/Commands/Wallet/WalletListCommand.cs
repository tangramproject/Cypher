// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;
using ConsoleTables;
using TGMWalletCore.Wallet;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "list" }, "Lists the wallets available")]
    class WalletListCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly IConsole _console;

        public WalletListCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            var keys = _walletService.WalletList();

            if (keys?.Any() == true)
            {
                var table = new ConsoleTable("Path");

                foreach (var key in keys)
                    table.AddRow(key);

                _console.WriteLine(table);
            }
            else
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("No wallets have been created.");
                _console.ForegroundColor = ConsoleColor.White;
            }

            return Task.CompletedTask;
        }
    }
}
