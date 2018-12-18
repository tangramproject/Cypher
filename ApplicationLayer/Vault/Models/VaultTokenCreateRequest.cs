using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault.Models
{
    public class VaultTokenCreateRequest
    {
        public List<string> policies { get; set; }
        public bool renewable { get; set; }
    }
}
