using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Commands.Vault;

namespace TangramCypher.ApplicationLayer.Commands
{
    public class CommandService : ICommandService
    {
        readonly IDictionary<string[], Type> commands;
        private bool prompt = true;
    
        public CommandService()
        {
            commands = new Dictionary<string[], Type>(new CommandEqualityComparer());

            RegisterCommands();
        }

        public void RegisterCommand<T>(string[] name) where T : ICommand
        {
            commands.Add(name, typeof(T));
        }

        public void RegisterCommands()
        {
            RegisterCommand<VaultDownloadCommand>(new string[] { "vault", "update" });
            RegisterCommand<VaultUnsealCommand>(new string[] { "vault", "unseal" });
            RegisterCommand<ExitCommand>(new string[] { "exit" });
            RegisterCommand<HelpCommand>(new string[] { "help" });
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

            if(command == null)
            {
                command = GetCommand(new string[] { "help" });
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

                await Execute(args);
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
