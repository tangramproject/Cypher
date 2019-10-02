// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.ComponentModel.DataAnnotations;

namespace TangramCypher.Model
{
    //TODO: Use byte array..
    public class CredentialsDto
    {
        [Required]
        public string Identifier { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
