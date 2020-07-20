// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using System.Threading.Tasks;

public class BackgroundQueue
{
    private Task _previousTask = Task.FromResult(true);
    private object _key = new object();

    public Task QueueTask(Action action)
    {
        lock (_key)
        {
            _previousTask = _previousTask.ContinueWith(t => action()
                , CancellationToken.None
                , TaskContinuationOptions.None
                , TaskScheduler.Default);
            return _previousTask;
        }
    }

    public Task<T> QueueTask<T>(Func<T> work)
    {
        lock (_key)
        {
            var task = _previousTask.ContinueWith(t => work()
                , CancellationToken.None
                , TaskContinuationOptions.None
                , TaskScheduler.Default);
            _previousTask = task;
            return task;
        }
    }
}