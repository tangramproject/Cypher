// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using LiteDB;
using Newtonsoft.Json;
using ProtoBuf;

namespace TangramCypher.Model
{
    public interface ICoinDto
    {
        EnvelopeDto Envelope { get; set; }
        string Hash { get; set; }
        string Hint { get; set; }
        string Keeper { get; set; }
        string Principle { get; set; }
        string Stamp { get; set; }
        string Network { get; set; }
        Guid TransactionId { get; set; }
        int Version { get; set; }

        D Cast<D>();
    }

    [ProtoContract]
    public class BaseCoinDto : ICoinDto
    {
        [ProtoMember(1)]
        public EnvelopeDto Envelope { get; set; }
        [ProtoMember(2)]
        public string Hash { get; set; }
        [ProtoMember(3)]
        public string Hint { get; set; }
        [ProtoMember(4)]
        public string Keeper { get; set; }
        [ProtoMember(5)]
        public string Principle { get; set; }
        [ProtoMember(6)]
        public string Stamp { get; set; }
        [ProtoMember(7)]
        public string Network { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
        [ProtoMember(8)]
        public int Version { get; set; }

        public D Cast<D>()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<D>(json);
        }
    }

    [ProtoContract]
    public class ReceiverCoinDto : BaseCoinDto
    {

    }

    [ProtoContract]
    public class SenderCoinDto : BaseCoinDto
    {

    }
}
