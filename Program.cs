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
using TangramCypher.ApplicationLayer.Wallet;
using Microsoft.Extensions.Configuration;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using TangramCypher.ApplicationLayer.Coin;
using System;
using TangramCypher.ApplicationLayer.Onion;
using TangramCypher.Model;
using TangramCypher.ApplicationLayer.Send;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TangramCypher
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File("Cypher.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting host");

                await CreateHostBuilder(args);

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static async Task CreateHostBuilder(string[] args)
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
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                    logging.AddSerilog();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services
                        .AddSingleton<IActorService, ActorService>()
                        .AddSingleton<ISendService, SendService>()
                        .AddSingleton<IWalletService, WalletService>()
                        .AddSingleton<IOnionServiceClient, OnionServiceClient>()
                        .AddSingleton<ICommandService, CommandService>()
                        .AddSingleton<ICoinService, CoinService>()
                        .AddSingleton<IHostedService, OnionService>()
                        .AddSingleton<IUnitOfWork, UnitOfWork>()
                        .AddSingleton<IHostedService, CommandService>(sp =>
                        {
                            return sp.GetService<ICommandService>() as CommandService;
                        });

                    services.AddSingleton<IHostedService, ApplicationLayer.HttpXy>();

                    services.Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));

                    var logger = new LoggerFactory().CreateLogger<Program>();

                    services.Add(new ServiceDescriptor(typeof(Microsoft.Extensions.Logging.ILogger),
                                                                provider => logger,
                                                                ServiceLifetime.Singleton));
                })
                .UseSerilog()
                .UseConsoleLifetime();

            await builder.RunConsoleAsync();
        }
    }
}
