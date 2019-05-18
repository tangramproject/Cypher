// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

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
        private readonly IVaultServiceClient vaultServiceClient;
 
        public VaultUnsealCommand(IServiceProvider serviceProvider)
        {
            vaultServiceClient = serviceProvider.GetService<IVaultServiceClient>();
        }

        public override async Task Execute()
        {
            using (var vaultShard = Prompt.GetPasswordAsSecureString("Vault Key:", ConsoleColor.Yellow))
            {
                await vaultServiceClient.Unseal(vaultShard);
            }
        }
    }
}
