// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

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