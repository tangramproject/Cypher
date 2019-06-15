// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace TangramCypher.Model
{
    public class KeySetDto
    {
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
        [PrimaryKey]
        public string Address { get; set; }
    }
}