// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Cypher.ApplicationLayer.Onion
{
    public interface IOnionService : IHostedService
    {
        Task<T> ClientGetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken);
        Task<IEnumerable<JObject>> GetRangeAsync(Uri baseAddress, string path, CancellationToken cancellationToken);
        Task<JObject> ClientPostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken);
        void ChangeCircuit(SecureString password);
        void GenerateHashPassword(SecureString password);
        void SendCommands(string command, SecureString password);
        void StartOnion();
    }
}
