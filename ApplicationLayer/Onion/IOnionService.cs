using System;
using System.Threading.Tasks;

namespace Cypher.ApplicationLayer.Onion
{
    public interface IOnionService
    {
        Task<string> GetAsync(string url, object data);
        void ChangeCircuit(string password);
        Task<T> PostAsync<T>(string url, object data) where T : class, new();
        void StartOnion(string password);
    }
}
