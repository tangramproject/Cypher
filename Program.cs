using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Commands;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers.LibSodium;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Cypher.ApplicationLayer.Onion;
using Microsoft.Extensions.Configuration;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

namespace TangramCypher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.SetBasePath(Directory.GetCurrentDirectory());

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services
                        .AddSingleton<IActorService, ActorService>()
                        .AddSingleton<IWalletService, WalletService>()
                        .AddTransient<ICryptography, Cryptography>()
                        .AddSingleton<IVaultService, VaultService>()
                        .AddSingleton<IOnionService, OnionService>()
                        .AddSingleton<IVaultService, VaultService>()
                        .AddSingleton<ICommandService, CommandService>()
                        .AddSingleton<IHostedService, OnionService>(sp =>
                        {
                            return sp.GetService<IOnionService>() as OnionService;
                        })
                        .AddSingleton<IHostedService, VaultService>(sp =>
                        {
                            return sp.GetService<IVaultService>() as VaultService;
                        })
                    .AddSingleton<IHostedService, CommandService>(sp =>
                    {
                        return sp.GetService<ICommandService>() as CommandService;
                    });

                    services.Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));


                    var logger = new LoggerFactory()
                                                .AddDebug()
                                                .AddFile("cypher.log")
                                                .CreateLogger("cypher");

                    services.Add(new ServiceDescriptor(typeof(ILogger),
                                                                provider => logger,
                                                                ServiceLifetime.Singleton));
                })
                .UseConsoleLifetime();

            await builder.RunConsoleAsync();
        }
    }
}
