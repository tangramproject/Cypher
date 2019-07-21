using NUnit.Framework;
using SimpleBase;

namespace Tangram.Address.UnitTests
{
    [TestFixture]
    public class V1TestnetAddressTests : AddressTests
    {
        protected override AddressVersion AddressVersion => AddressVersion.V1Testnet;
        protected override byte[] WalletAddress => Base16.Decode("0A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20212223242526272829").ToArray();
        protected override byte[] NetworkAddress => Base16.Decode("000A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20212223242526272829").ToArray();
        protected override string TangramAddress => "tgm_0185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MG40WBM183";
        protected override string TangramAddressWithoutPrefix => "0185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MG40WBM183";
        protected override string TangramAddressWithWrongPrefix => "tgn_0185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MG40WBM183";
        protected override string TangramAddressWithWrongVersion => "tgm_Z185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MG40WBM183";
        protected override string TangramAddressWithWrongBody => "tgm_0285GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MG40WBM183";
        protected override string TangramAddressWithWrongChecksum => "tgm_0185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MG50WBM183";
    }
}
