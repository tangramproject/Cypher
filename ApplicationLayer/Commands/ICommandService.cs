using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Commands
{
    public interface ICommandService
    {
        void RegisterCommand<T>(string[] name) where T : ICommand;
        void Execute(string[] args);
        void InteractiveCliLoop();
    }
}
