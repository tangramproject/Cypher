// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using TGMWalletCore.Wallet;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "key" }, "Creates a new key")]
    public class WalletAddressCommand : Command
    {
        private readonly IConsole _console;
        private readonly IWalletService _walletService;

        public WalletAddressCommand(IServiceProvider serviceProvider)
        {
            _console = serviceProvider.GetService<IConsole>();
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow))
                {
                    try
                    {
                        _walletService.AddKeySet(passphrase, identifier);

                        _console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine("\nWallet Key added!\n");
                        _console.ForegroundColor = ConsoleColor.White;
                    }
                    catch (Exception ex)
                    {
                        _console.ForegroundColor = ConsoleColor.Red;
                        _console.WriteLine($"{ex.Message}\n");
                        _console.ForegroundColor = ConsoleColor.White;
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
