using NUnit.Framework;
using SimpleBase;

namespace Tangram.Address.UnitTests
{
    [TestFixture]
    public class V1MainnetAddressTests : AddressTests
    {
        protected override AddressVersion AddressVersion => AddressVersion.V1Mainnet;
        protected override byte[] WalletAddress => Base16.Decode("DB38CC023FFC5AC30329DF9D00EE4B7C5AF9D517B806FCE7E4764359409C4643").ToArray();
        protected override byte[] NetworkAddress => Base16.Decode("01DB38CC023FFC5AC30329DF9D00EE4B7C5AF9D517B806FCE7E4764359409C4643").ToArray();
        protected override string TangramAddress => "tgm_1VCWCR0HZZHDC60S9VYEG1VJBFHDFKN8QQ03FSSZ4ES1NJG4W8S1G25TPSAJX";
        protected override string TangramAddressWithoutPrefix => "1VCWCR0HZZHDC60S9VYEG1VJBFHDFKN8QQ03FSSZ4ES1NJG4W8S1G25TPSAJX";
        protected override string TangramAddressWithWrongPrefix => "tgn_1VCWCR0HZZHDC60S9VYEG1VJBFHDFKN8QQ03FSSZ4ES1NJG4W8S1G25TPSAJX";
        protected override string TangramAddressWithWrongVersion => "tgm_ZVCWCR0HZZHDC60S9VYEG1VJBFHDFKN8QQ03FSSZ4ES1NJG4W8S1G25TPSAJX";
        protected override string TangramAddressWithWrongBody => "tgm_1WCWCR0HZZHDC60S9VYEG1VJBFHDFKN8QQ03FSSZ4ES1NJG4W8S1G25TPSAJX";
        protected override string TangramAddressWithWrongChecksum => "tgm_1VCWCR0HZZHDC60S9VYEG1VJBFHDFKN8QQ03FSSZ4ES1NJG4W8S1G35TPSAJX";
    }
}
