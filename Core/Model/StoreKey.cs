// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Tangram.Core.Actor;

namespace Tangram.Core.Model
{
    public class StoreKey
    {
        private readonly string name;
        private readonly int value;

        public static readonly StoreKey AddressKey = new StoreKey(1, Constant.AddressKey);
        public static readonly StoreKey PublicKey = new StoreKey(2, Constant.PublicKey);
        public static readonly StoreKey SecretKey = new StoreKey(3, Constant.SecretKey);
        public static readonly StoreKey HashKey = new StoreKey(4, Constant.Hash);
        public static readonly StoreKey TransactionIdKey = new StoreKey(5, "TransactionId");

        private StoreKey(int value, string name)
        {
            this.value = value;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}