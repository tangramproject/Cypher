using System;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
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

            logger.LogDebug("Starting application");

        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IActorService, ActorService>()
                .AddSingleton<IWalletService, WalletService>()
                .AddTransient<ICryptography, Cryptography>();
        }
    }
}
