// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.Model
{
    public class StoreName
    {
        private readonly string name;
        private readonly int value;

        public static readonly StoreName Transactions = new StoreName(1, "transactions");
        public static readonly StoreName Track = new StoreName(2, "track");
        public static readonly StoreName StoreKeys = new StoreName(3, "storeKeys");
        public static readonly StoreName Redemption = new StoreName(4, "redemption");
        public static readonly StoreName Receiver = new StoreName(4, "receiver");
        public static readonly StoreName PublicKeyAgreement = new StoreName(4, "publickeyagreement");

        private StoreName(int value, string name)
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