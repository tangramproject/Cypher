using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Commands;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers.ServiceLocator;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceLocator locator = new Locator();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            locator.Add<IServiceProvider, ServiceProvider>(serviceProvider);

            var commandService = serviceProvider.GetService<ICommandService>();

            while (commandService.PromptLoop())
            {

            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IActorService, ActorService>()
                .AddSingleton<IWalletService, WalletService>()
                .AddTransient<ICryptography, Cryptography>()
                .AddSingleton<ICommandService, CommandService>();
        }
    }
}
