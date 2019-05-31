// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
namespace TangramCypher.ApplicationLayer.Wallet
{
    public enum TransactionType
    {
        Send,
        Receive
    }

    public class TransactionDto
    {
        public string TransactionId { get; set; }
        public ulong Amount { get; set; }
        public string Blind { get; set; }
        public string Commitment { get; set; }
        public string Hash { get; set; }
        public string Stamp { get; set; }
        public int Version { get; set; }
        public TransactionType TransactionType { get; set; }
        public string Memo { get; set; }
        public DateTime DateTime { get; set; }
    }
}
