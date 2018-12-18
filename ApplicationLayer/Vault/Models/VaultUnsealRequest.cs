using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault.Models
{
    public class VaultUnsealRequest
    {
        public string key { get; set; }
        public bool reset { get; set; }
    }
}
