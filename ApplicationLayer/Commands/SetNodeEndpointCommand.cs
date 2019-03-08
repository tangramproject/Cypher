// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "setnodeendpoint" }, "Set contact endpoint")]
    public class SetNodeEndpointCommand : Command
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task Execute()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException();
        }
    }
}
