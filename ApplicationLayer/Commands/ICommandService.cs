using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands
{
    public interface ICommandService : IHostedService
    {
        void RegisterCommand<T>(string[] name) where T : ICommand;
        Task Execute(string[] args);
        Task Exit();
    }
}
