namespace Tangram.Address
{
    public class AddressBuilderV1Testnet : AddressBuilderV1
    {
        public override AddressVersion Version => AddressVersion.V1Testnet;
        public override string TextualVersion => "0";
        public override byte[] BinaryVersion => new byte[] { 0 };
        public override string TextualChecksumSeed => "Tangram testnet checksum V1 | Just as a cell doesn't define us, so we aren't just a thought or an instinct.";
    }
}
