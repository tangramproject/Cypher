using McMaster.Extensions.CommandLineUtils;

namespace TangramCypher.ApplicationLayer.Commands
{
    public class HelpCommand : Command
    {
        public override void Execute()
        {
            PhysicalConsole.Singleton.WriteLine("[list commands here]");
        }
    }
}