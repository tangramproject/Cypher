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
using Microsoft.Extensions.Logging;
using TangramCypher.Helper;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "get" }, "Retrieves the contents of a wallet")]
    class WalletGetCommand : Command
    {
        private IVaultService vaultService;
        private IConsole console;
        private ILogger logger;

        public WalletGetCommand(IServiceProvider serviceProvider)
        {
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
            logger = serviceProvider.GetService<ILogger>();
        }

        public override async Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
                {

                    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                    {
                        using (var id = identifier.Insecure())
                        {
                            var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{id.Value}/wallet");

                            var w = JsonConvert.SerializeObject(data);

                            console.WriteLine(w);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception");
                throw;
            }
        }
    }
}
