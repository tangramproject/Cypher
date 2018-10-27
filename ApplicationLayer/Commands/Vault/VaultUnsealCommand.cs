using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helpers.ServiceLocator;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    public class VaultUnsealCommand : Command
    {
        private readonly IVaultService vaultService;
 
        public VaultUnsealCommand()
        {
            var serviceProvider = Locator.Instance.GetService<IServiceProvider>();
            vaultService = serviceProvider.GetService<IVaultService>();
        }

        public override void Execute()
        {
            var vaultShard = Prompt.GetPassword("Vault Shard:", ConsoleColor.Yellow);
            vaultService.Unseal(vaultShard);
        }
    }
}
