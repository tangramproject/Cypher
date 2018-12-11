using System;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "list" }, "Lists the wallets available")]
    class WalletListCommand : Command
    {
        private IVaultService vaultService;
        private IConsole console;

        public WalletListCommand(IServiceProvider serviceProvider)
        {
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            var data = await vaultService.GetListAsync($"wallets/");

            var keys = data.Data?.Keys;

            if (keys != null)
            {
                foreach (var key in data.Data?.Keys)
                {
                    var k = key.TrimEnd('/');

                    console.WriteLine(k);
                }
            }
        }
    }
}
