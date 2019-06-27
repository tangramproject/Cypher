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
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "create" }, "Creates a new wallet")]
    class WalletCreateCommand : Command
    {
        private readonly IConsole console;
        private readonly IWalletService walletService;

        public WalletCreateCommand(IServiceProvider serviceProvider)
        {
            console = serviceProvider.GetService<IConsole>();
            walletService = serviceProvider.GetService<IWalletService>();
        }

        public override Task Execute()
        {
            var creds = walletService.CreateWallet();
            console.WriteLine($"Created Wallet {creds.Identifier} with password: {creds.Password}");

            return Task.CompletedTask;
        }
    }
}
