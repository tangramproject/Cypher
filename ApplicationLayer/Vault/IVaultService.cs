using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Vault
{
    public interface IVaultService
    {
        void StartVaultService();
        void Unseal(string shard);
    }
}
