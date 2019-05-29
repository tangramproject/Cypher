// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helper;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "key" }, "Creates a new key set")]
    public class WalletAddressCommand : Command
    {
        readonly IConsole console;
        readonly IWalletService walletService;

        public WalletAddressCommand(IServiceProvider serviceProvider)
        {
            console = serviceProvider.GetService<IConsole>();
            walletService = serviceProvider.GetService<IWalletService>();
        }

        public async override Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
                {
                    var pksk = walletService.CreatePkSk();
                    var added = await walletService.Put(identifier, password, pksk.Address, pksk, "storeKeys", "Address");

                    if(added)
                    {
                        console.ForegroundColor = ConsoleColor.Magenta;
                        console.WriteLine("\nWallet Key set added!\n");
                        console.ForegroundColor = ConsoleColor.White;

                        return;
                    }

                    console.ForegroundColor = ConsoleColor.Red;
                    console.WriteLine("Something went wrong!");
                    console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
