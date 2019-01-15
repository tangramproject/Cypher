using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault.Models
{
    public class VaultIdentityEntityCreateRequest
    {
        public string name { get; set; }
        public string[] policies { get; set; }
    }
}
