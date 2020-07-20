// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using LiteDB;
using ProtoBuf;

namespace Tangram.Core.Model
{
    [ProtoContract]
    public class Coin : ICoin
    {
        [BsonId]
        public Guid TransactionId { get; set; }

        [ProtoMember(1)]
        public int Ver { get; set; }
        [ProtoMember(2)]
        public string PreImage { get; set; }
        [ProtoMember(3)]
        public int Mix { get; set; }
        [ProtoMember(4)]
        public Vin Vin { get; set; }
        [ProtoMember(5)]
        public Vout Vout { get; set; }
    }
}
