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
    [CommandDescriptor(new string[] { "wallet", "get" }, "Retrieves the contents of a wallet")]
    class WalletGetCommand : Command
    {
        private IVaultService vaultService;
        private IConsole console;

        public WalletGetCommand()
        {
            var serviceProvider = Locator.Instance.GetService<IServiceProvider>();
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            var identifier = Prompt.GetPassword("Identifier:", ConsoleColor.Yellow);
            var password = Prompt.GetPassword("Password:", ConsoleColor.Yellow);

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{identifier}/wallet");

                var w = JsonConvert.SerializeObject(data);

                console.WriteLine(w);
            }
        }
    }
}
