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
    public class BlockIDDto
    {
        [ProtoMember(1)]
        public string Hash;
        [ProtoMember(2)]
        public ulong Node;
        [ProtoMember(3)]
        public ulong Round;
        [ProtoMember(4)]
        public BlockDto SignedBlock;
    }
}
