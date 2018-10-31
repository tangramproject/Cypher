using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using TangramCypher.ApplicationLayer.Commands.Vault;
using TangramCypher.ApplicationLayer.Commands.Wallet;

namespace TangramCypher.ApplicationLayer.Commands
{
    public class CommandService : ICommandService
    {
        private readonly IConsole console;
        private readonly ILogger logger;
        readonly IDictionary<string[], Type> commands;
        private bool prompt = true;

        public CommandService(IConsole cnsl, ILogger lgr)
        {
            console = cnsl;
            logger = lgr;

            commands = new Dictionary<string[], Type>(new CommandEqualityComparer());

            RegisterCommands();
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
                command = Activator.CreateInstance(commandType) as ICommand;
            }

            return command;
        }

        public async Task Execute(string[] args)
        {
            var command = GetCommand(args);

            if (command == null)
            {
                console.WriteLine();
                console.WriteLine("  Commands");

                foreach (var cmd in commands)
                {
                    var commandDescriptor = cmd.Value.GetCustomAttribute<CommandDescriptorAttribute>();
                    var name = string.Join(' ', commandDescriptor.Name);
                    command = Activator.CreateInstance(cmd.Value) as ICommand;
                    console.WriteLine($"    {name}".PadRight(25) + $"{commandDescriptor.Description}");
                }

                return;
            }

            await command.Execute();
        }

        public async Task InteractiveCliLoop()
        {
            while (prompt)
            {
                var args = Prompt.GetString("tangram$", promptColor: ConsoleColor.Cyan)?.Split(' ');

                if (args == null)
                    continue;

                try
                {
                    await Execute(args);
                }
                catch (Exception e)
                {
                    console.BackgroundColor = ConsoleColor.Red;
                    console.ForegroundColor = ConsoleColor.White;
                    console.WriteLine(e.ToString());
                    logger.LogError(e, Environment.NewLine);
                    console.ResetColor();
                }
            }
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
