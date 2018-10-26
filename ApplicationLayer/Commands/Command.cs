using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Commands
{
    public abstract class Command : ICommand
    {
        public abstract void Execute();
    }
}
