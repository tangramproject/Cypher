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
using System.Threading.Tasks;

namespace TangramCypher.Model
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<bool> Put(SecureString identifier, SecureString password, StoreKey name, string key, TEntity value);
        Task<TEntity> Get(SecureString identifier, SecureString password, StoreKey name, string key);
        Task<IEnumerable<TEntity>> All(SecureString identifier, SecureString password);
        Task<bool> Truncate(SecureString identifier, SecureString password);
        Task<bool> AddOrReplace(SecureString identifier, SecureString password, StoreKey name, string key, TEntity value);
        Task<bool> Delete(SecureString identifier, SecureString password, StoreKey name, string key);
    }
}