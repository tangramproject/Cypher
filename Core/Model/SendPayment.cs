// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace Tangram.Core.Model
{
    public class SendPayment
    {
        public Credentials  Credentials { get; set; }
        public double Amount { get; set; }
        public string Address { get; set; }
        public bool CreateRedemptionKey { get; set; }
        public string Memo { get; set; }
    }
}
