using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "vault", "unseal" }, "Begins the Vault unseal process")]
    public class VaultUnsealCommand : Command
    {
        private readonly IVaultService vaultService;
 
        public VaultUnsealCommand(IServiceProvider serviceProvider)
        {
            vaultService = serviceProvider.GetService<IVaultService>();
        }

        public override async Task Execute()
        {
            var vaultShard = Prompt.GetPassword("Vault Shard:", ConsoleColor.Yellow);
            await vaultService.Unseal(vaultShard);
        }
    }
}
