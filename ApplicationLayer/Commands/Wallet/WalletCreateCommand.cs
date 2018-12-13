using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers;
using TangramCypher.ApplicationLayer.Vault;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "create" }, "Creates a new wallet")]
    class WalletCreateCommand : Command
    {
        readonly IConsole console;
        readonly IWalletService walletService;
        readonly IVaultService vaultService;

        public WalletCreateCommand(IServiceProvider serviceProvider)
        {
            console = serviceProvider.GetService<IConsole>();
            walletService = serviceProvider.GetService<IWalletService>();
            vaultService = serviceProvider.GetService<IVaultService>();
        }

        public override async Task Execute()
        {
            var walletId = walletService.NewID(16);
            var passphrase = walletService.Passphrase();
            var pkSk = walletService.CreatePkSk();

            walletId.MakeReadOnly();
            passphrase.MakeReadOnly();

            try
            {
                await vaultService.CreateUserAsync(walletId.ToUnSecureString(), passphrase.ToUnSecureString());

                // TODO: Add list for multiple store keys.
                //var dic = new Dictionary<string, object>
                //{
                //    { "storeKeys", new List<PkSkDto> { pkSk } }
                //};

                var dic = new Dictionary<string, object>
                {
                    { "storeKeys", pkSk  }
                };

                await vaultService.SaveDataAsync(
                    walletId.ToUnSecureString(),
                    passphrase.ToUnSecureString(),
                    $"wallets/{walletId.ToUnSecureString()}/wallet",
                    dic);

                console.WriteLine($"Created Wallet {walletId.ToUnSecureString()} with password: {passphrase.ToUnSecureString()}");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                walletId.Dispose();
                passphrase.Dispose();
            }

        }
    }
}
