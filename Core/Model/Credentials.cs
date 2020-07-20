// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.ComponentModel.DataAnnotations;

namespace Tangram.Core.Model
{
    //TODO: Use byte array..
    public class Credentials
    {
        [Required]
        public string Identifier { get; set; }
        [Required]
        public string[] Mnemonic { get; set; }
        public string Passphrase { get; set; }
    }
}
