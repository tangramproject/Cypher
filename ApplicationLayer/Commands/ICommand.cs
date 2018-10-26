using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Commands
{
    public interface ICommand
    {
        void Execute();
    }
}
