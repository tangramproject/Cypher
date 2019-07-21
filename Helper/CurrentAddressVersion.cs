// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Tangram.Address;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.Helper
{
	public static class CurrentAddressVersion
	{
		public static AddressVersion Get(string environment, NetworkApiMethod networkApi)
		{
			var env = networkApi == null ? environment : networkApi.ToString();

			return env == Constant.Mainnet ? AddressVersion.V1Mainnet : AddressVersion.V1Testnet;
		}
	}
}
