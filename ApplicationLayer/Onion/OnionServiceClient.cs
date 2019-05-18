using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

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
