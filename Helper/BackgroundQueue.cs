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

public class BackgroundQueue
{
    private Task previousTask = Task.FromResult(true);
    private object key = new object();

    public Task QueueTask(Action action)
    {
        lock (key)
        {
            previousTask = previousTask.ContinueWith(t => action()
                , CancellationToken.None
                , TaskContinuationOptions.None
                , TaskScheduler.Default);
            return previousTask;
        }
    }

    public Task<T> QueueTask<T>(Func<T> work)
    {
        lock (key)
        {
            var task = previousTask.ContinueWith(t => work()
                , CancellationToken.None
                , TaskContinuationOptions.None
                , TaskScheduler.Default);
            previousTask = task;
            return task;
        }
    }
}