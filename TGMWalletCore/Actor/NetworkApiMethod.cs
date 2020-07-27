// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Actor
{
    public class NetworkApiMethod
    {
        private readonly string _name;
        private readonly int _value;

        public static readonly NetworkApiMethod Mainnet = new NetworkApiMethod(1, Constant.Mainnet);
        public static readonly NetworkApiMethod Testnet = new NetworkApiMethod(2, Constant.Testnet);

        private NetworkApiMethod(int value, string name)
        {
            _value = value;
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
