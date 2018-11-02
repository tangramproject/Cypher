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
using Microsoft.Extensions.Configuration;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging.Console;
using System.Reflection;
using TangramCypher.ApplicationLayer.Commands.Exceptions;

namespace TangramCypher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                IServiceLocator locator = new Locator();

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection
                    .AddLogging()
                    .BuildServiceProvider();

                var logger = serviceProvider.GetService<ILogger>();

                logger.LogInformation("Starting Application");

                locator.Add<IServiceProvider, ServiceProvider>(serviceProvider);

                var onionService = serviceProvider.GetService<IOnionService>();

                // Testing onion...
                onionService.StartOnion(onionService.GenerateHashPassword("ILoveTangram"));

                var commandService = serviceProvider.GetService<ICommandService>();
                var vaultService = serviceProvider.GetService<IVaultService>();

                await vaultService.StartVaultServiceAsync();
                await commandService.InteractiveCliLoop();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IActorService, ActorService>()
                .AddSingleton<IWalletService, WalletService>()
                .AddTransient<ICryptography, Cryptography>()
                .AddSingleton<IVaultService, VaultService>()
                .AddSingleton<ICommandService, CommandService>()
                .AddSingleton<IOnionService, OnionService>()
                .Add(new ServiceDescriptor(typeof(IConfiguration),
                     provider => new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json",
                                                 optional: false,
                                                 reloadOnChange: true)
                                    .Build(),
                     ServiceLifetime.Singleton));

            var logger = new LoggerFactory()
                                        .AddDebug()
                                        .AddFile("cypher.log")
                                        .CreateLogger("cypher");

            serviceCollection.Add(new ServiceDescriptor(typeof(ILogger),
                                                        provider => logger,
                                                        ServiceLifetime.Singleton));
            serviceCollection.Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));
        }
    }
}
