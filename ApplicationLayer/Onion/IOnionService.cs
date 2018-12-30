using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Cypher.ApplicationLayer.Onion
{
    public interface IOnionService : IHostedService
    {
        Task<T> ClientGetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken);
        Task<JObject> ClientPostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken);
        void ChangeCircuit(SecureString password);
        void GenerateHashPassword(SecureString password);
        void SendCommands(string command, SecureString password);
        void StartOnion();
    }
}
