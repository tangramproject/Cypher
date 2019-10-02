// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using LiteDB;

namespace TangramCypher.Model
{
    public class MessageStoreDto
    {
        public DateTime DateTime { get; set; }
        public string Hash { get; set; }
        public string Memo { get; set; }
        public MessageDto Message { get; set; }
        public string PublicKey { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}
