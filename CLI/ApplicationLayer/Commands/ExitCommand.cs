// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "exit" }, "Exits the wallet")]
    public class ExitCommand : Command
    {
        ICommandService commandService;

        public ExitCommand(IServiceProvider provider)
        {
            commandService = provider.GetService<ICommandService>();
        }

        public Task<bool> asdasd()
        {
            return Task.FromResult(true);
        }

        public override async Task Execute()
        {
            await commandService.Exit();
        }
    }
}
