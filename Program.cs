// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Commands;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helper.LibSodium;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Cypher.ApplicationLayer.Onion;
using Microsoft.Extensions.Configuration;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using TangramCypher.ApplicationLayer.Coin;
using System;

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
                        .AddSingleton<IOnionService, OnionService>()
                        .AddSingleton<IVaultService, VaultService>()
                        .AddSingleton<ICommandService, CommandService>()
                        .AddSingleton<ICoinService, CoinService>()
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
