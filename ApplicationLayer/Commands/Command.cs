using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands
{
    public abstract class Command : ICommand
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public abstract Task Execute();
    }
}
