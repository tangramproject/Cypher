// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using TGMWalletCore.Actor;

namespace TGMWalletCore.Model
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