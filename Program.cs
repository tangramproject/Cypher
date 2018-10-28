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
<<<<<<< HEAD
using Cypher.ApplicationLayer.Onion;
=======
using Microsoft.Extensions.Configuration;
using System.IO;
>>>>>>> 1a8c7b169d056161ea81b201a08293de8683ab46

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

            await commandService.InteractiveCliLoop();
        }

        static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IActorService, ActorService>()
                .AddSingleton<IWalletService, WalletService>()
                .AddTransient<ICryptography, Cryptography>()
                .AddSingleton<IVaultService, VaultService>()
                .AddSingleton<ICommandService, CommandService>()
<<<<<<< HEAD
                .AddSingleton<IOnionService, OnionService>();
=======
                .Add(new ServiceDescriptor(typeof(IConfiguration),
                     provider => new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json",
                                                 optional: false,
                                                 reloadOnChange: true)
                                    .Build(),
                     ServiceLifetime.Singleton));
>>>>>>> 1a8c7b169d056161ea81b201a08293de8683ab46
        }
    }
}
