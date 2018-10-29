using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helpers.ServiceLocator;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    class WalletCreateCommand : Command
    {
        private IVaultService vaultService;
        private IConsole console;

        public WalletCreateCommand()
        {
            var serviceProvider = Locator.Instance.GetService<IServiceProvider>();
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            //  TODO: Call WalletService instead
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[8];

                rng.GetBytes(bytes);

                var username = BitConverter.ToUInt64(bytes, 0).ToString();

                rng.GetBytes(bytes);

                var password = BitConverter.ToUInt64(bytes, 0).ToString();

                await vaultService.CreateUserAsync(username, password);

                IDictionary<string, object> dic = new Dictionary<string, object>();

                dic.Add("somedata", new { a = 1, b = 2 });

                await vaultService.SaveDataAsync(username, password, $"wallets/{username}/wallet", dic);

                console.WriteLine($"Created Wallet {username} with password {password}");
            }
        }
    }
}
