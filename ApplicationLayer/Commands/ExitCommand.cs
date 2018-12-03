using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "exit" }, "Exits the wallet")]
    public class ExitCommand : Command
    {
        ICommandService commandService;

        public ExitCommand(IServiceProvider provider)
        {
            commandService = provider.GetService<ICommandService>();
        }

        public override async Task Execute()
        {
            commandService.Exit();
        }
    }
}
