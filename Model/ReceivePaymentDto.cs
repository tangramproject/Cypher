// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.ComponentModel.DataAnnotations;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Model;

namespace TangramCypher.Model
{
    public class ReceivePaymentDto
    {
        public CredentialsDto Credentials { get; set; }
        [Required]
        public string FromAddress { get; set; }
        public MessageDto RedemptionMessage { get; set; }
    }
}
