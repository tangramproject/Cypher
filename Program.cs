using System;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Commands;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers.ServiceLocator;
using TangramCypher.Helpers.LibSodium;
using Microsoft.Extensions.Logging;

namespace TangramCypher
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceLocator locator = new Locator();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection
                .AddLogging()
                .BuildServiceProvider();


            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

            locator.Add<IServiceProvider, ServiceProvider>(serviceProvider);

<<<<<<< HEAD
            logger.LogDebug("Starting application");

=======
            var commandService = serviceProvider.GetService<ICommandService>();
            var vaultService = serviceProvider.GetService<IVaultService>();

            vaultService.StartVaultService();

            while (commandService.PromptLoop());
>>>>>>> 0c60ac672ddd1208d065dee9f4a773bad7d035c2
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IActorService, ActorService>()
                .AddSingleton<IWalletService, WalletService>()
                .AddTransient<ICryptography, Cryptography>()
                .AddSingleton<IVaultService, VaultService>()
                .AddSingleton<ICommandService, CommandService>();
        }
    }
}
