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
using Tangram.Core.Wallet;
using ConsoleTables;
using System.Linq;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "get" }, "Retrieves the contents of a wallet")]
    class WalletGetCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly IConsole _console;

        public WalletGetCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var addresses = _walletService.ListAddresses(identifier, passphrase);

            if (addresses?.Any() == true)
            {
                var table = new ConsoleTable("Address");

                foreach (var address in addresses)
                    table.AddRow(address);

                _console.WriteLine(table);
            }
            else
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("No addresses have been created.");
                _console.ForegroundColor = ConsoleColor.White;
            }

            return Task.CompletedTask;
        }
    }
}
