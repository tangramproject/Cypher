// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace Tangram.Core.Actor
{
    public enum State
    {
        Audited,
        Burned,
        Keys,
        Committed,
        Agree,
        Redemption,
        Track,
        Payment,
        Holder,
        Owner,
        New,
        Completed,
        RedemptionKey,
        PublicKeyAgree,
        Failure,
        Reversed
    }
}