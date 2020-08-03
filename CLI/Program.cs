// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.Extensions.DependencyInjection;
using Tangram.Bamboo.ApplicationLayer.Commands;
using Microsoft.Extensions.Configuration;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using System;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using TGMWalletCore.Actor;
using TGMWalletCore.Send;
using TGMWalletCore.Wallet;
using TGMWalletCore.Coin;

namespace Tangram.Bamboo
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File("Bamboo.log", rollingInterval: RollingInterval.Day)
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
                        .AddSingleton<ICommandService, CommandService>()
                        .AddSingleton<IBuilderService, BuilderService>()
                        .AddSingleton<IHostedService, CommandService>(sp =>
                        {
                            return sp.GetService<ICommandService>() as CommandService;
                        })
                        .AddLogging(config =>
                        {
                            config.ClearProviders();
                            config.AddProvider(new SerilogLoggerProvider(Log.Logger));
                        });

                    services.Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));
                })
                .UseSerilog((context, configuration) => configuration
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.File("Bamboo.log", rollingInterval: RollingInterval.Day))
                .UseConsoleLifetime();

            await builder.RunConsoleAsync();
        }
    }
}
