// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    public class WalletAddressCommand : Command
    {
        public WalletAddressCommand()
        {
        }

        public override Task Execute()
        {
            Console.WriteLine("Method not implemented!");

            return Task.CompletedTask;
        }
    }
}
