// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Model
{
    public class StoreName
    {
        private readonly string name;
        private readonly int value;

        public static readonly StoreName Transactions = new StoreName(1, "transactions");
        public static readonly StoreName Track = new StoreName(2, "track");
        public static readonly StoreName StoreKeys = new StoreName(3, "storeKeys");
        public static readonly StoreName Redemption = new StoreName(4, "redemption");
        public static readonly StoreName Receiver = new StoreName(5, "receiver");
        public static readonly StoreName PublicKeyAgreement = new StoreName(6, "publickeyagreement");
        public static readonly StoreName Purchase = new StoreName(7, "purchase");
        public static readonly StoreName Sender = new StoreName(8, "sender");
        public static readonly StoreName Queue = new StoreName(9, "queue");

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