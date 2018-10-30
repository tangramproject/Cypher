using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helpers.ServiceLocator;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    class WalletListCommand : Command
    {
        private IVaultService vaultService;
        private IConsole console;

        public WalletListCommand()
        {
            var serviceProvider = Locator.Instance.GetService<IServiceProvider>();
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            var data = await vaultService.GetListAsync($"wallets/");
        }
    }
}
