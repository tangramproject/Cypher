using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Onion
{
    public interface IOnionServiceClient
    {
        int OnionEnabled { get; }
        string SocksHost { get; }
        int SocksPort { get; }
    }
}
