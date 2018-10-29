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
