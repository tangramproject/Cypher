using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VaultSharp.V1.Commons;

namespace TangramCypher.ApplicationLayer.Vault
{
    public interface IVaultService
    {
        Task StartVaultServiceAsync();
        Task Unseal(string shard, bool skipPrint = false);
        Task CreateUserAsync(string username, string password);
        Task SaveDataAsync(string username, string password, string path, IDictionary<string, object> data);
        Task<Secret<SecretData>> GetDataAsync(string username, string password, string path);
        Task<Secret<ListInfo>> GetListAsync(string path);
    }
}
