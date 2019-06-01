// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class TransactionCoin
    {
        public ulong Balance { get; set; }
        public string Blind { get; set; }
        public IList<TransactionDto> Chain { get; set; }
        public ulong Input { get; set; }
        public ulong Output { get; set; }
        public bool Spent { get; set; }
        public string Stamp { get; set; }
        public int Version { get; set; }
    }
}
