// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;

namespace TangramCypher.Model
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TaskResult<bool>> Put(Session session, TEntity value);
        Task<TaskResult<TEntity>> Get(Session session, StoreKey name, string key);
        Task<TaskResult<IEnumerable<TEntity>>> All(Session session);
        Task<TaskResult<bool>> Truncate(Session session);
        Task<TaskResult<bool>> AddOrReplace(Session session, TEntity value);
        Task<TaskResult<bool>> Delete(Session session, StoreKey name, string key);
    }
}