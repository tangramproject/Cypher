// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Vault
{
    //[CommandDescriptor(new string[] { "setnodeendpoint" }, "Set contact endpoint")]
    public class SetNodeEndpointCommand : Command
    {
        public override Task Execute() => throw new NotImplementedException();
    }
}
