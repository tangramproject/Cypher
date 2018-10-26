using McMaster.Extensions.CommandLineUtils;
using System;
using Vault;
using Vault.Endpoints.Sys;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    public class VaultUnsealCommand : Command
    {
        public override void Execute()
        {
            var vaultShard = Prompt.GetPassword("Vault Shard:", ConsoleColor.Yellow);
            var vaultOptions = VaultOptions.Default;

            vaultOptions.Address = "http://127.0.0.1:8200";

            var vaultClient = new VaultClient(vaultOptions);

            var shard = vaultShard.ToString();

            var unsealTask = vaultClient.Sys.Unseal(shard);
            unsealTask.Wait();

            var response = unsealTask.Result;

            if (!response.Sealed)
            {
                PhysicalConsole.Singleton.ResetColor();
                PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.DarkGreen;
                PhysicalConsole.Singleton.WriteLine("Vault Unsealed!");
            }
        }
    }
}
