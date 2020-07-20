// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using LiteDB;

namespace Tangram.Core.Model
{
    public class Transaction
    {
        public string Address { get; set; }
        public ulong Balance { get; set; }
        public DateTime DateTime { get; set; }
        public string EphemKey { get; set; }
        public ulong Input { get; set; }
        public string Memo { get; set; }
        public ulong Output { get; set; }
        public bool Spent { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
        public TransactionType TransactionType { get; set; }
    }
}