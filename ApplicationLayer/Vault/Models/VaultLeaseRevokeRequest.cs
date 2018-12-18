using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault.Models
{
    public class VaultLeaseRevokeRequest
    {
        public string lease_id { get; set; }
    }
}
