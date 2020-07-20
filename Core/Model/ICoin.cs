// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;

namespace Tangram.Core.Model
{
    public interface ICoin
    {
        Guid TransactionId { get; set; }
        int Ver { get; set; }
        string PreImage { get; set; }
        int Mix { get; set; }
        Vin Vin { get; set; }
        Vout Vout { get; set; }
    }
}