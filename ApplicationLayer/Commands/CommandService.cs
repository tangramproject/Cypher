using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TangramCypher.ApplicationLayer.Commands.Exceptions;
using TangramCypher.ApplicationLayer.Commands.Vault;
using TangramCypher.ApplicationLayer.Commands.Wallet;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helper;

namespace TangramCypher.ApplicationLayer.Commands
{
    public class CommandService : HostedService, ICommandService
    {
        private readonly IConsole console;
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        readonly IDictionary<string[], Type> commands;
        private bool prompt = true;

        private Thread _t;

        public CommandService(IConsole cnsl, IServiceProvider provider, ILogger lgr)
        {
            console = cnsl;
            logger = lgr;
            serviceProvider = provider;

            commands = new Dictionary<string[], Type>(new CommandEqualityComparer());

            console.CancelKeyPress += Console_CancelKeyPress;

            RegisterCommands();
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            ExitCleanly().GetAwaiter().GetResult();
        }

        public async Task Exit()
        {
            await ExitCleanly();
        }

        public void RegisterCommand<T>(string[] name) where T : ICommand
        {
            commands.Add(name, typeof(T));
        }

        public void RegisterCommand(string[] name, Type t)
        {
            if (typeof(ICommand).IsAssignableFrom(t))
            {
                commands.Add(name, t);
                return;
            }

            throw new ArgumentException("Command must implement ICommand interface", nameof(t));
        }

        public void RegisterCommands()
        {
            var commands = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass
                                                                               && typeof(Command).IsAssignableFrom(x)
                                                                               && x.GetCustomAttribute<CommandDescriptorAttribute>() != null
                                                                               ).OrderBy(x => string.Join(' ', x.GetCustomAttribute<CommandDescriptorAttribute>().Name));

            foreach (var command in commands)
            {
                var attribute = command.GetCustomAttribute<CommandDescriptorAttribute>() as CommandDescriptorAttribute;

                RegisterCommand(attribute.Name, command);
            }
        }

        private ICommand GetCommand(string[] args)
        {
            var cmd = args.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            ICommand command = null;

            if (commands.ContainsKey(cmd))
            {
                var commandType = commands[cmd];

                var cstr = commandType.GetConstructor(new Type[] { typeof(IServiceProvider) });

                if (cstr != null)
                {
                    command = Activator.CreateInstance(commandType, serviceProvider) as ICommand;
                }
                else
                {
                    command = Activator.CreateInstance(commandType) as ICommand;
                }
            }

            return command;
        }

        public async Task Execute(string[] args)
        {
            var command = GetCommand(args);

            if (command == null)
            {
                PrintHelp();
                return;
            }

            await command.Execute();
        }

        private void PrintHelp()
        {
            console.WriteLine();
            console.WriteLine("  Commands");

            foreach (var cmd in commands)
            {
                var commandDescriptor = cmd.Value.GetCustomAttribute<CommandDescriptorAttribute>();
                var name = string.Join(' ', commandDescriptor.Name);

                console.WriteLine($"    {name}".PadRight(25) + $"{commandDescriptor.Description}");
            }
        }

        public async Task InteractiveCliLoop()
        {
            await StartAllHostedProviders();

            while (prompt)
            {
                var args = Prompt.GetString("tangram$", promptColor: ConsoleColor.Cyan)?.TrimEnd()?.Split(' ');

                if (args == null || (args.Length == 1 && string.IsNullOrEmpty(args[0])))
                    continue;

                try
                {
                    await Execute(args);
                }
                catch (Exception e)
                {
                    Util.LogException(console, logger, e);
                }
            }

            await ExitCleanly();
        }

        private async Task ExitCleanly()
        {
            prompt = false;

            console.WriteLine("Exiting...");

            await StopAllHostedProviders();

            Environment.Exit(0);
        }

        private IEnumerable<Type> FindAllHostedServiceTypes()
        {
            //  Concrete Service Types

            var type = typeof(HostedService);
            var concreteServiceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p != GetType() && p != typeof(HostedService));

            //  Find interfaces that implement IHostedService
            foreach (var concreteServiceType in concreteServiceTypes)
            {
                var interfaces = concreteServiceType.GetInterfaces();

                foreach (var inf in interfaces)
                {
                    if (inf == typeof(IHostedService))
                    {
                        continue;
                    }

                    var implements = inf.GetInterfaces().Any(x => x == typeof(IHostedService));

                    if (implements)
                    {
                        yield return inf;
                    }
                }
            }
        }

        private async Task StartAllHostedProviders()
        {
            var hostedProviders = FindAllHostedServiceTypes();

            foreach (var hostedProvider in hostedProviders)
            {
                var serviceInstance = serviceProvider.GetService(hostedProvider) as IHostedService;

                if (serviceInstance != null)
                {
                    await serviceInstance.StartAsync(new CancellationToken());
                }
            }
        }

        private async Task StopAllHostedProviders()
        {
            var hostedProviders = FindAllHostedServiceTypes();

            foreach (var hostedProvider in hostedProviders)
            {
                var serviceInstance = serviceProvider.GetService(hostedProvider) as IHostedService;

                if (serviceInstance != null)
                {
                    await serviceInstance.StopAsync(new CancellationToken());
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                _t = new Thread(async () => { await InteractiveCliLoop(); });
                _t.Start();
            });
        }
    }

    public class CommandEqualityComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(string[] obj)
        {
            int result = 17;

            for (int i = 0; i < obj.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i].GetHashCode();
                }
            }

            return result;
        }
    }
}
