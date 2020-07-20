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
    public class Purchase
    {
        public ulong Balance { get; set; }
        public DateTime DateTime { get; set; }
        public string EphemKey { get; set; }
        public ulong Input { get; set; }
        public ulong Output { get; set; }
        public bool Spent { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}
