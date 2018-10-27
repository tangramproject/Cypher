using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Vault
{
    public interface IVaultService
    {
        Task StartVaultServiceAsync();
        Task Unseal(string shard);
    }
}
