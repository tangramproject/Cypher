using System;
using System.Threading;
using System.Threading.Tasks;

namespace TangramCypher.Helper.ShellProgress
{
    public interface IProgressBarBase
    {
        Task Start(CancellationToken token);
    }
}
