// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace TangramCypher.ApplicationLayer.Actor
{
    public sealed class RestApiMethod
    {
        private readonly string name;
        private readonly int value;

        public static readonly RestApiMethod PostCoin = new RestApiMethod(1, Constant.PostCoin);
        public static readonly RestApiMethod Coin = new RestApiMethod(2, Constant.GetCoin);
        public static readonly RestApiMethod Message = new RestApiMethod(3, Constant.GetMessage);
        public static readonly RestApiMethod PostMessage = new RestApiMethod(4, Constant.PostMessage);
        public static readonly RestApiMethod Messages = new RestApiMethod(5, Constant.GetMessages);
        public static readonly RestApiMethod MessageRange = new RestApiMethod(6, Constant.GetMessageRange);
        public static readonly RestApiMethod MessageCount = new RestApiMethod(7, Constant.GetMessageCount);

        private RestApiMethod(int value, string name)
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
