// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TangramCypher.ApplicationLayer.Onion
{
    public class OnionServiceClient : IOnionServiceClient
    {
        public int OnionEnabled { get; }
        public string SocksHost { get; }
        public int SocksPort { get; }

        readonly IConfigurationSection onionSection;

        public OnionServiceClient(IConfiguration configuration, ILogger logger, IConsole console)
        {
            onionSection = configuration.GetSection(Constants.ONION);

            SocksHost = onionSection.GetValue<string>(Constants.SOCKS_HOST);
            SocksPort = onionSection.GetValue<int>(Constants.SOCKS_PORT);
            OnionEnabled = onionSection.GetValue<int>(Constants.ONION_ENABLED);
        }
    }
}
