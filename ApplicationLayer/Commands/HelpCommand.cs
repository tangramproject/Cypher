// Cypher (c) by Tangram LLC
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

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