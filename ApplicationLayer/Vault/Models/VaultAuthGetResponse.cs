// Cypher (c) by Tangram LLC
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault.Models
{
    public class VaultAuthGetResponseConfig
    {
        public int default_lease_ttl { get; set; }
        public int max_lease_ttl { get; set; }
        public string plugin_name { get; set; }
    }

    public class VaultAuthGetResponseToken
    {
        public string accessor { get; set; }
        public VaultAuthGetResponseConfig config { get; set; }
        public string description { get; set; }
        public bool local { get; set; }
        public object options { get; set; }
        public bool seal_wrap { get; set; }
        public string type { get; set; }
    }

    public class VaultAuthGetResponseConfig2
    {
        public int default_lease_ttl { get; set; }
        public int max_lease_ttl { get; set; }
        public string plugin_name { get; set; }
    }

    public class VaultAuthGetResponseOptions
    {
    }

    public class VaultAuthGetResponseUserpass
    {
        public string accessor { get; set; }
        public VaultAuthGetResponseConfig2 config { get; set; }
        public string description { get; set; }
        public bool local { get; set; }
        public VaultAuthGetResponseOptions options { get; set; }
        public bool seal_wrap { get; set; }
        public string type { get; set; }
    }

    public class VaultAuthGetResponseConfig3
    {
        public int default_lease_ttl { get; set; }
        public int max_lease_ttl { get; set; }
        public string plugin_name { get; set; }
    }

    public class VaultAuthGetResponseToken2
    {
        public string accessor { get; set; }
        public VaultAuthGetResponseConfig3 config { get; set; }
        public string description { get; set; }
        public bool local { get; set; }
        public object options { get; set; }
        public bool seal_wrap { get; set; }
        public string type { get; set; }
    }

    public class VaultAuthGetResponseConfig4
    {
        public int default_lease_ttl { get; set; }
        public int max_lease_ttl { get; set; }
        public string plugin_name { get; set; }
    }

    public class VaultAuthGetResponseOptions2
    {
    }

    public class VaultAuthGetResponseUserpass2
    {
        public string accessor { get; set; }
        public VaultAuthGetResponseConfig4 config { get; set; }
        public string description { get; set; }
        public bool local { get; set; }
        public VaultAuthGetResponseOptions2 options { get; set; }
        public bool seal_wrap { get; set; }
        public string type { get; set; }
    }

    public class VaultAuthGetResponseData
    {
        public VaultAuthGetResponseToken2 __invalid_name__token { get; set; }
        public VaultAuthGetResponseUserpass2 __invalid_name__userpass { get; set; }
    }

    public class VaultAuthGetResponse
    {
        public VaultAuthGetResponseToken __invalid_name__token { get; set; }
        public VaultAuthGetResponseUserpass __invalid_name__userpass { get; set; }
        public string request_id { get; set; }
        public string lease_id { get; set; }
        public bool renewable { get; set; }
        public int lease_duration { get; set; }
        public VaultAuthGetResponseData data { get; set; }
        public object wrap_info { get; set; }
        public object warnings { get; set; }
        public object auth { get; set; }
    }
}
