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
using TGMWalletCore.Helper;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "balance" }, "Get current wallet balance")]
    public class WalletBalanceCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly IConsole _console;

        public WalletBalanceCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow))
                {
                    var total = _walletService.AvailableBalance(identifier, passphrase);

                    _console.ForegroundColor = ConsoleColor.Magenta;
                    _console.WriteLine($"\nWallet balance: {total.Result.DivWithNaT():F9}\n");
                    _console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Task.CompletedTask;
        }
    }
}
