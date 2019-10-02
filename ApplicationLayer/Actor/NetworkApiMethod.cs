// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace TangramCypher.ApplicationLayer.Actor
{
    public class NetworkApiMethod
    {
        private readonly string name;
        private readonly int value;

        public static readonly NetworkApiMethod Mainnet = new NetworkApiMethod(1, Constant.Mainnet);
        public static readonly NetworkApiMethod Testnet = new NetworkApiMethod(2, Constant.Testnet);

        private NetworkApiMethod(int value, string name)
        {
            this.value = value;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
