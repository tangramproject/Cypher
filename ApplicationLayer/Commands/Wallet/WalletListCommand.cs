// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "list" }, "Lists the wallets available")]
    class WalletListCommand : Command
    {
        private readonly IWalletService walletService;
        private readonly IConsole console;

        public WalletListCommand(IServiceProvider serviceProvider)
        {
            walletService = serviceProvider.GetService<IWalletService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            var keys = await walletService.WalletList();

            if (keys != null)
            {
                foreach (var key in keys)
                {
                    var k = key.TrimEnd('/');

                    console.WriteLine(k);
                }
            }
        }
    }
}
