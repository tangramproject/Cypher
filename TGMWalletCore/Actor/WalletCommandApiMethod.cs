// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Actor
{
    public class WalletCommandApiMethod
    {
        private readonly string _name;
        private readonly int _value;

        public static readonly WalletCommandApiMethod Send = new WalletCommandApiMethod(1, "Send");
        public static readonly WalletCommandApiMethod Receive = new WalletCommandApiMethod(2, "Receive");

        private WalletCommandApiMethod(int value, string name)
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
