// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using TGMWalletCore.Wallet;
using TGMWalletCore.Helper;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "create" }, "Creates a new wallet")]
    class WalletCreateCommand : Command
    {
        private readonly IConsole _console;
        private readonly IWalletService _walletService;

        public WalletCreateCommand(IServiceProvider serviceProvider)
        {
            _console = serviceProvider.GetService<IConsole>();
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public async override Task Execute()
        {
            var yesNo = Prompt.GetYesNo("Create default mnemonic and passphrase?", true, ConsoleColor.Yellow);

            try
            {
                if (yesNo)
                {
                    var mnemonicDefault = await _walletService.CreateMnemonic(NBitcoin.Language.English, NBitcoin.WordCount.TwentyFour);
                    var passphraseDefault = await _walletService.CreateMnemonic(NBitcoin.Language.English, NBitcoin.WordCount.Twelve);
                    var joinMmnemonic = string.Join(" ", mnemonicDefault);
                    var joinPassphrase = string.Join(" ", passphraseDefault);
                    var id = _walletService.CreateWallet(joinMmnemonic.ToSecureString(), joinPassphrase.ToSecureString());
                    var path = Util.WalletPath(id);

                    _console.WriteLine();

                    _console.WriteLine("Wallet Path:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{path}");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();

                    _console.WriteLine("Wallet ID:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{id}");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();

                    _console.WriteLine("Seed phrase:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{joinMmnemonic}");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();

                    _console.WriteLine("Two-Factor:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{joinPassphrase}");
                    _console.ForegroundColor = ConsoleColor.White;
                   

                    _console.WriteLine();

                    joinMmnemonic.ZeroString();
                    joinPassphrase.ZeroString();
                }
                else
                {
                    using var mnemonic = Prompt.GetPasswordAsSecureString("Mnemonic:", ConsoleColor.Yellow);
                    using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

                    var id = _walletService.CreateWallet(mnemonic, passphrase);
                    var path = Util.WalletPath(id);

                    _console.WriteLine($"Wallet ID: {id}");
                    _console.WriteLine($"Wallet Path: {path}");
                }
            }
            catch (Exception ex)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"Please create the mnemonics first. See help for more info.\n {ex.Message}");
                _console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
