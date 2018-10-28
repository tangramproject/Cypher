using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands
{
    public class HelpCommand : Command
    {
        public override async Task Execute()
        {
            PhysicalConsole.Singleton.WriteLine("[list commands here]");
        }
    }
}