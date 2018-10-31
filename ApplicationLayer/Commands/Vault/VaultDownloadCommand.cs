using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "vault", "download" }, "Downloads the latest version of Vault")]
    public class VaultDownloadCommand : Command
    {
        public override async Task Execute() => throw new NotImplementedException();
    }
}
