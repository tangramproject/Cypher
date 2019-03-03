// Cypher (c) by Tangram LLC
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class TransactionChange
    {
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
        public TransactionDto Transaction { get; set; }
        public double AmountFor { get; set; }
    }
}
