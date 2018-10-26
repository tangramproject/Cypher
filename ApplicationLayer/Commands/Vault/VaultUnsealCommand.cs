using McMaster.Extensions.CommandLineUtils;
using System;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helpers.ServiceLocator;
using Vault;
using Vault.Endpoints.Sys;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    public class VaultUnsealCommand : Command
    {
        private readonly IVaultService vaultService;
 
        public VaultUnsealCommand()
        {
        }

        public override void Execute()
        {
            var vaultShard = Prompt.GetPassword("Vault Shard:", ConsoleColor.Yellow);
            vaultService.Unseal(vaultShard);
        }
    }
}
