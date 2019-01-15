using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault.Models
{
    public class VaultCreateEntityAliasRequest
    {
        public string name { get; set; }
        public string canonical_id { get; set; }
        public string mount_accessor { get; set; }
    }
}
