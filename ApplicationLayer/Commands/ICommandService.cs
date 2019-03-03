// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

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
