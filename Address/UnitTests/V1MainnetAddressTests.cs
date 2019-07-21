using NUnit.Framework;
using SimpleBase;

namespace Tangram.Address.UnitTests
{
    [TestFixture]
    public class V1MainnetAddressTests : AddressTests
    {
        protected override AddressVersion AddressVersion => AddressVersion.V1Mainnet;
        protected override byte[] WalletAddress => Base16.Decode("0A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20212223242526272829").ToArray();
        protected override byte[] NetworkAddress => Base16.Decode("010A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20212223242526272829").ToArray();
        protected override string TangramAddress => "tgm_1185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MGSFNWEJAB";
        protected override string TangramAddressWithoutPrefix => "1185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MGSFNWEJAB";
        protected override string TangramAddressWithWrongPrefix => "tgn_1185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MGSFNWEJAB";
        protected override string TangramAddressWithWrongVersion => "tgm_Z185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MGSFNWEJAB";
        protected override string TangramAddressWithWrongBody => "tgm_1285GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MGSFNWEJAB";
        protected override string TangramAddressWithWrongChecksum => "tgm_1185GR38E1W8124GK2GAHC5RR34D1P70X3RFJ08924CJ2A9H750MGTFNWEJAB";
    }
}
