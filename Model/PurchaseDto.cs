// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using TangramCypher.Model;

namespace TangramCypher.Model
{
    public class PurchaseDto
    {
        public ulong Balance { get; set; }
        public string Blind { get; set; }
        public HashSet<Guid> Chain { get; set; }
        public DateTime DateTime { get; set; }
        public ulong Input { get; set; }
        public ulong Output { get; set; }
        public bool Spent { get; set; }
        public string Stamp { get; set; }
        [PrimaryKey]
        public Guid TransactionId { get; set; }
        public int Version { get; set; }
    }
}
