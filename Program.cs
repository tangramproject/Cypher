using System;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Commands;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers.ServiceLocator;
using TangramCypher.Helpers.LibSodium;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Cypher.ApplicationLayer.Onion;

namespace TangramCypher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IServiceLocator locator = new Locator();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection
                .AddLogging()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

            locator.Add<IServiceProvider, ServiceProvider>(serviceProvider);

            logger.LogDebug("Starting application");

            var commandService = serviceProvider.GetService<ICommandService>();
            var vaultService = serviceProvider.GetService<IVaultService>();

            await vaultService.StartVaultServiceAsync();

            commandService.InteractiveCliLoop();
        }

        static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IActorService, ActorService>()
                .AddSingleton<IWalletService, WalletService>()
                .AddTransient<ICryptography, Cryptography>()
                .AddSingleton<IVaultService, VaultService>()
                .AddSingleton<ICommandService, CommandService>()
                .AddSingleton<IOnionService, OnionService>();
        }
    }
}
