// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace TangramCypher.ApplicationLayer.Coin
{
    public class ReceiverOutput
    {
        public double Amount { get; private set; }
        public byte[] Commit { get; private set; }
        public byte[] Blind { get; private set; }

        public ReceiverOutput(double amount, byte[] commit, byte[] blind)
        {
            Amount = amount;
            Commit = commit;
            Blind = blind;
        }
    }
}
