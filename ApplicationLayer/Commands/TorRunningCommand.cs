// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Onion;

namespace TangramCypher.ApplicationLayer.Commands.Tor
{
    [CommandDescriptor(new string[] { "tor", "running" }, "Check tor running state")]
    public class TorRunningCommand : Command
    {
        readonly IOnionServiceClient onionServiceClient;
        readonly IConsole console;

        public TorRunningCommand(IServiceProvider serviceProvider)
        {
            console = serviceProvider.GetService<IConsole>();
            onionServiceClient = serviceProvider.GetService<IOnionServiceClient>();
        }

        public override Task Execute()
        {
            if (onionServiceClient.OnionEnabled == 1)
            {
                switch (onionServiceClient.IsTorRunning())
                {
                    case true:
                        console.ForegroundColor = ConsoleColor.Magenta;
                        console.WriteLine("\nTor Bootstrapped 100%\n");
                        console.ForegroundColor = ConsoleColor.White;
                        break;
                    default:
                        console.ForegroundColor = ConsoleColor.Yellow;
                        console.WriteLine("\nTor could still be starting.. Wait for a few seconds and try again.\n");
                        console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                return Task.CompletedTask;
            }


            console.ForegroundColor = ConsoleColor.Red;
            console.WriteLine("\nTor is not enabled.\n");
            console.ForegroundColor = ConsoleColor.White;

            return Task.CompletedTask;
        }
    }
}
