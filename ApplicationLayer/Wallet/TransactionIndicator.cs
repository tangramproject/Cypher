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
    public class TransactionIndicator
    {
        public double AmountFor { get; set; }
        public double Change { get; set; }
        public int NextVersion { get; set; }
        public string Stamp { get; set; }
        public TransactionDto Transaction { get; set; }
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    }
}
