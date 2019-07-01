using NUnit.Framework;
using SimpleBase;

namespace Tangram.Address.UnitTests
{
    [TestFixture]
    public class V1MainnetAddressTests : AddressTests
    {
        protected override AddressVersion AddressVersion => AddressVersion.V1Mainnet;
        protected override byte[] WalletAddress => Base16.Decode("BAE878C833C01ACE2A01BB897ED911D16048CF152F326B9361D1BF101898D52B").ToArray();
        protected override byte[] NetworkAddress => Base16.Decode("01BAE878C833C01ACE2A01BB897ED911D16048CF152F326B9361D1BF101898D52B").ToArray();
        protected override string TangramAddress => "tgm_1QBM7HJ1KR0DCWAG1QE4QXP8HT5G4HKRN5WS6Q4V1T6ZH064RTMNGBCWYZ08";
        protected override string TangramAddressWithoutPrefix => "1QBM7HJ1KR0DCWAG1QE4QXP8HT5G4HKRN5WS6Q4V1T6ZH064RTMNGBCWYZ08";
        protected override string TangramAddressWithWrongPrefix => "tgn_1QBM7HJ1KR0DCWAG1QE4QXP8HT5G4HKRN5WS6Q4V1T6ZH064RTMNGBCWYZ08";
        protected override string TangramAddressWithWrongVersion => "tgm_ZQBM7HJ1KR0DCWAG1QE4QXP8HT5G4HKRN5WS6Q4V1T6ZH064RTMNGBCWYZ08";
        protected override string TangramAddressWithWrongBody => "tgm_1RBM7HJ1KR0DCWAG1QE4QXP8HT5G4HKRN5WS6Q4V1T6ZH064RTMNGBCWYZ08";
        protected override string TangramAddressWithWrongChecksum => "tgm_1QBM7HJ1KR0DCWAG1QE4QXP8HT5G4HKRN5WS6Q4V1T6ZH064RTMNGCCWYZ08";
    }
}
