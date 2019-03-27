// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using VaultSharp.V1.Commons;

namespace TangramCypher.ApplicationLayer.Vault
{
    public interface IVaultService
    {
        Task StartVaultService();
        Task Unseal(SecureString shard, bool skipPrint = false);
        Task CreateUserAsync(SecureString username, SecureString password);
        Task SaveDataAsync(SecureString username, SecureString password, string path, IDictionary<string, object> data);
        Task<Secret<Dictionary<string, object>>> GetDataAsync(SecureString username, SecureString password, string path);
        Task<Secret<ListInfo>> GetListAsync(string path);
    }
}
