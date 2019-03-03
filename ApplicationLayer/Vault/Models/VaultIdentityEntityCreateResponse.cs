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
    public class VaultIdentityEntityCreateResponseData
    {
        public object aliases { get; set; }
        public string id { get; set; }
    }

    public class VaultIdentityEntityCreateResponse
    {
        public string request_id { get; set; }
        public string lease_id { get; set; }
        public bool renewable { get; set; }
        public int lease_duration { get; set; }
        public VaultIdentityEntityCreateResponseData data { get; set; }
        public object wrap_info { get; set; }
        public object warnings { get; set; }
        public object auth { get; set; }
    }
}
