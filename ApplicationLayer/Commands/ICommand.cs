using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands
{
    public interface ICommand
    {
        string Name { get; set; }
        string Description { get; set; }
        Task Execute();
    }
}
