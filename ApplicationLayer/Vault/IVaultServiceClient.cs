using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using VaultSharp.V1.Commons;

namespace TangramCypher.ApplicationLayer.Vault
{
    public interface IVaultServiceClient
    {
        Task Unseal(SecureString shard, bool skipPrint = false);
        Task CreateUserAsync(SecureString username, SecureString password);
        Task SaveDataAsync(SecureString username, SecureString password, string path, IDictionary<string, object> data);
        Task<Secret<Dictionary<string, object>>> GetDataAsync(SecureString username, SecureString password, string path);
        Task<Secret<ListInfo>> GetListAsync(string path);
        Task<T> PostAsJsonAsync<T>(object obj, string requestUri, SecureString authToken = null);
        Task<T> PutAsJsonAsync<T>(object obj, string requestUri, SecureString authToken = null);
        Task<T> GetAsJsonAsync<T>(string requestUri, SecureString authToken = null);
    }
}