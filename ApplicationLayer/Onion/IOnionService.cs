// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.Extensions.Hosting;
using System.Security;

namespace Cypher.ApplicationLayer.Onion
{
    public interface IOnionService : IHostedService
    {
        bool OnionStarted { get; }
        string SocksHost { get; }
        int SocksPort { get; }
        int ControlPort { get; }
        int OnionEnabled { get; }

        void ChangeCircuit(SecureString password);
        void Dispose();
        void GenerateHashPassword(SecureString password);
        void SendCommands(string command, SecureString password);
        void StartOnion();
    }
}
