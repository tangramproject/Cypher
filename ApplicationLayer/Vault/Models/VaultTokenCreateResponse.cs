// Cypher (c) by Tangram Inc
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
    public class VaultTokenCreateResponseAuth
    {
        public string client_token { get; set; }
        public string accessor { get; set; }
        public List<string> policies { get; set; }
        public List<string> token_policies { get; set; }
        public object metadata { get; set; }
        public int lease_duration { get; set; }
        public bool renewable { get; set; }
        public string entity_id { get; set; }
    }

    public class VaultTokenCreateResponse
    {
        public string request_id { get; set; }
        public string lease_id { get; set; }
        public bool renewable { get; set; }
        public int lease_duration { get; set; }
        public object data { get; set; }
        public object wrap_info { get; set; }
        public object warnings { get; set; }
        public VaultTokenCreateResponseAuth auth { get; set; }
    }
}
