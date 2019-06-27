// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using ProtoBuf;

namespace TangramCypher.Model
{
    [ProtoContract]
    public class EnvelopeDto
    {
        [ProtoMember(1)]
        public string Commitment { get; set; }
        [ProtoMember(2)]
        public string Proof { get; set; }
        [ProtoMember(3)]
        public string PublicKey { get; set; }
        [ProtoMember(4)]
        public string Signature { get; set; }
        [ProtoMember(5)]
        public string RangeProof { get; set; }
    }
}
