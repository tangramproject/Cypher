using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "create" }, "Creates a new wallet")]
    class WalletCreateCommand : Command
    {
        private IVaultService vaultService;
        private IConsole console;
        readonly IWalletService walletService;

        public WalletCreateCommand(IServiceProvider serviceProvider)
        {
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
            walletService = serviceProvider.GetService<IWalletService>();
        }

        public override async Task Execute()
        {
            //  TODO: Call WalletService instead
            //using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            //{
            //    var bytes = new byte[8];

            //    rng.GetBytes(bytes);

            //    var username = BitConverter.ToUInt64(bytes, 0).ToString();

            //    rng.GetBytes(bytes);

            //    var password = BitConverter.ToUInt64(bytes, 0).ToString();

            //    await vaultService.CreateUserAsync(username, password);

            //    IDictionary<string, object> dic = new Dictionary<string, object>();

            //    dic.Add("somedata", new { a = 1, b = 2 });

            //    await vaultService.SaveDataAsync(username, password, $"wallets/{username}/wallet", dic);

            //    console.WriteLine($"Created Wallet {username} with password {password}");
            //}

            var walletId = walletService.NewID(16);
            var passphrase = walletService.Passphrase();
            var pkSk = walletService.CreatePkSk();

            await vaultService.CreateUserAsync(walletId, passphrase);

            var dic = new Dictionary<string, object>
            {
                { "storeKeys", pkSk }
            };

            await vaultService.SaveDataAsync(walletId, passphrase, $"wallets/{walletId}/wallet", dic);

            console.WriteLine($"Created Wallet {walletId} with password: {passphrase}");
        }
    }
}
