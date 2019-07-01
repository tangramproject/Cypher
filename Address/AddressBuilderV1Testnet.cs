namespace Tangram.Address
{
    public class AddressBuilderV1Testnet : AddressBuilderV1
    {
        public override AddressVersion Version => AddressVersion.V1Testnet;
        public override string TextualVersion => "0";
        public override byte[] BinaryVersion => new byte[] { 0 };
        public override string TextualBodySeed => "Tangram testnet body V1 | Just as a cell doesn't define us, so we aren't just a thought or an instinct.";
        public override string TextualChecksumSeed => "Tangram testnet checksum V1 | There is no war against machines. Humans want to be machines: no privacy, no secrets, no individualism, no differences. An algorithm with binary emotions, sad or happy, the beginnings of artificial intelligence.";
    }
}
