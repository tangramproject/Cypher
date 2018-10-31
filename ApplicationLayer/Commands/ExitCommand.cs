using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.Helpers.ServiceLocator;
using Microsoft.Extensions.DependencyInjection;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "exit" }, "Exits the wallet")]
    public class ExitCommand : Command
    {
        ICommandService commandService;

        public ExitCommand()
        {
            var serviceProvider = Locator.Instance.GetService<IServiceProvider>();
            commandService = serviceProvider.GetService<ICommandService>();
        }

        public override async Task Execute()
        {
            commandService.Exit();
        }
    }
}
