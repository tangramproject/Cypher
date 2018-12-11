using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Cypher.ApplicationLayer.Onion
{
    public interface IOnionService : IHostedService
    {
        Task<string> GetAsync(string url, object data);
        void ChangeCircuit(string password);
        string GenerateHashPassword(string password);
        Task<T> PostAsync<T>(string url, object data) where T : class, new();
        void StartOnion(string password);
    }
}
