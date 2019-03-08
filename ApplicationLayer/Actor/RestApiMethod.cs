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

        public static readonly RestApiMethod PostBlock = new RestApiMethod(1, Constant.PostBlock);
        public static readonly RestApiMethod BlockMerkle = new RestApiMethod(2, Constant.GetBlockMerkle);
        public static readonly RestApiMethod PostCoin = new RestApiMethod(3, Constant.PostCoin);
        public static readonly RestApiMethod Coin = new RestApiMethod(4, Constant.GetCoin);
        public static readonly RestApiMethod Coins = new RestApiMethod(5, Constant.GetCoins);
        public static readonly RestApiMethod Transaction = new RestApiMethod(6, Constant.GetTransaction);
        public static readonly RestApiMethod TransactionCount = new RestApiMethod(7, Constant.GetTransactionCount);
        public static readonly RestApiMethod TransactionRange = new RestApiMethod(8, Constant.GetTransactionRange);
        public static readonly RestApiMethod VerifiyShortTransactions = new RestApiMethod(9, Constant.GetVerifiyShortTransactions);
        public static readonly RestApiMethod Message = new RestApiMethod(10, Constant.GetMessage);
        public static readonly RestApiMethod PostMessage = new RestApiMethod(11, Constant.PostMessage);
        public static readonly RestApiMethod Messages = new RestApiMethod(12, Constant.GetMessages);
        public static readonly RestApiMethod MessageRange = new RestApiMethod(13, Constant.GetMessageRange);
        public static readonly RestApiMethod MessageCount = new RestApiMethod(14, Constant.GetMessageCount);

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
