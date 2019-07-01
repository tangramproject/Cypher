namespace Tangram.Address
{
    public class AddressBuilderV1Mainnet : AddressBuilderV1
    {
        public override AddressVersion Version => AddressVersion.V1Mainnet;
        public override string TextualVersion => "1";
        public override byte[] BinaryVersion => new byte[] { 1 };
        public override string TextualBodySeed => "Tangram mainnet body V1 | You are not alone. We don't bow to monopoly of any kind, be it of justice, morality or economy. You can be sure that we will always promote freedom of choice. You'll have the opportunity to educate yourself, to learn, respect and promote diversity. Giants will stand behind you so that you can decide for yourself what follows next.";
        public override string TextualChecksumSeed => "Tangram mainnet checksum V1 | We learn our entire life. We fight for control, we attack the systems created by others, we defend the systems we create. Such fights eat away time and energy, and only rarely improve our understanding of the human mind and of the world. The wasted energy could have been used for true learning, for personal control, for evolution.";
    }
}
