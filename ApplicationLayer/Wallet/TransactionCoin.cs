// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class TransactionCoin
    {
        public IList<TransactionDto> Chain { get; set; }
        public double Balance { get; set; }
        public double Input { get; set; }
        public double Output { get; set; }
        public bool Spent { get; set; }
        public string Stamp { get; set; }
        public int Version { get; set; }
    }
}
