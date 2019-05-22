using System;
namespace TangramCypher.ApplicationLayer.Actor
{
    public class StoreKeyApiMethod
    {
        private readonly string name;
        private readonly int value;

        public static readonly StoreKeyApiMethod AddressKey = new StoreKeyApiMethod(1, Constant.AddressKey);
        public static readonly StoreKeyApiMethod PublicKey = new StoreKeyApiMethod(2, Constant.PublicKey);
        public static readonly StoreKeyApiMethod SecretKey = new StoreKeyApiMethod(3, Constant.SecretKey);

        private StoreKeyApiMethod(int value, string name)
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
