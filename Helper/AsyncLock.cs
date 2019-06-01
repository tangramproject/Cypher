// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace TangramCypher.Helper
{
    public class AsyncLock : IDisposable
    {
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<AsyncLock> LockAsync()
        {
            await semaphoreSlim.WaitAsync();
            return this;
        }

        public void Dispose()
        {
            semaphoreSlim.Release();
        }
    }
}