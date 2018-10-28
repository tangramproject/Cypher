using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands
{
    public interface ICommand
    {
        Task Execute();
    }
}
