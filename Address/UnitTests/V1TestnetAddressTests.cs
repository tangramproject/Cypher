using NUnit.Framework;
using SimpleBase;

namespace Tangram.Address.UnitTests
{
    [TestFixture]
    public class V1TestnetAddressTests : AddressTests
    {
        protected override AddressVersion AddressVersion => AddressVersion.V1Testnet;
        protected override byte[] WalletAddress => Base16.Decode("E255F03E9D16AD4886B2B944883F703DA65428D82B460B1AA5145FDD9279A98B").ToArray();
        protected override byte[] NetworkAddress => Base16.Decode("00E255F03E9D16AD4886B2B944883F703DA65428D82B460B1AA5145FDD9279A98B").ToArray();
        protected override string TangramAddress => "tgm_0W9AZ0FMX2TPMH1NJQ528GFVG7PK58A6R5D30P6N52HFXV4KSN65GJYHKB8GZ";
        protected override string TangramAddressWithoutPrefix => "0W9AZ0FMX2TPMH1NJQ528GFVG7PK58A6R5D30P6N52HFXV4KSN65GJYHKB8GZ";
        protected override string TangramAddressWithWrongPrefix => "tgn_0W9AZ0FMX2TPMH1NJQ528GFVG7PK58A6R5D30P6N52HFXV4KSN65GJYHKB8GZ";
        protected override string TangramAddressWithWrongVersion => "tgm_ZW9AZ0FMX2TPMH1NJQ528GFVG7PK58A6R5D30P6N52HFXV4KSN65GJYHKB8GZ";
        protected override string TangramAddressWithWrongBody => "tgm_0X9AZ0FMX2TPMH1NJQ528GFVG7PK58A6R5D30P6N52HFXV4KSN65GJYHKB8GZ";
        protected override string TangramAddressWithWrongChecksum => "tgm_0W9AZ0FMX2TPMH1NJQ528GFVG7PK58A6R5D30P6N52HFXV4KSN65GKYHKB8GZ";
    }
}
