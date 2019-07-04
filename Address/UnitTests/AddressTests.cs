using NUnit.Framework;
using System.Diagnostics;
using System.Linq;
using Tangram.Address.Exceptions;

namespace Tangram.Address.UnitTests
{
    [TestFixture]
    public abstract class AddressTests
    {
        protected readonly byte[] PublicKey = Enumerable.Range(10, 32).Select(x => (byte)x).ToArray();

        protected abstract AddressVersion AddressVersion { get; }
        protected abstract byte[] WalletAddress { get; }
        protected abstract byte[] NetworkAddress { get; }
        protected abstract string TangramAddress { get; }
        protected abstract string TangramAddressWithoutPrefix { get; }
        protected abstract string TangramAddressWithWrongPrefix { get; }
        protected abstract string TangramAddressWithWrongVersion { get; }
        protected abstract string TangramAddressWithWrongBody { get; }
        protected abstract string TangramAddressWithWrongChecksum { get; }

        protected readonly AddressBuilderFactory Factory = new AddressBuilderFactory();

        [Test]
        public void BuildWalletAddress()
        {
            var walletAddress = Factory.BuildWalletAddressFromPublicKey(PublicKey, AddressVersion);

            Trace.WriteLine($"Address version = '{AddressVersion}'"
                + $", wallet address = '{SimpleBase.Base16.EncodeUpper(walletAddress.ToArray())}'");

            Assert.AreEqual(WalletAddress, walletAddress.ToArray());
        }

        [Test]
        public void BuildNetworkAddress()
        {
            var networkAddress = Factory.BuildNetworkAddressFromPublicKey(PublicKey, AddressVersion);

            Trace.WriteLine($"Address version = '{AddressVersion}'"
                + $", network address = '{SimpleBase.Base16.EncodeUpper(networkAddress.ToArray())}'");

            Assert.AreEqual(NetworkAddress, networkAddress.ToArray());
        }

        [Test]
        public void EncodeTangramAddress()
        {
            var walletAddress = Factory.BuildWalletAddressFromPublicKey(PublicKey, AddressVersion);
            var tangramAddress = Factory.Encode(walletAddress, AddressVersion);

            Trace.WriteLine($"Address version = '{AddressVersion}', tangram address = '{tangramAddress}'");

            Assert.AreEqual(TangramAddress, tangramAddress);
        }

        [Test]
        public void ParseTangramAddress()
        {
            AddressParts addressParts = Factory.TryDecodeAddressPartsVerify(TangramAddress);

            Assert.IsNotNull(addressParts);
            Assert.AreEqual(WalletAddress, addressParts.Body);
        }

        [Test]
        public void ParseLowerCaseTangramAddress()
        {
            AddressParts addressParts = Factory.TryDecodeAddressPartsVerify(TangramAddress.ToLowerInvariant());

            Assert.IsNotNull(addressParts);
            Assert.AreEqual(WalletAddress, addressParts.Body);
        }

        [Test]
        public void ParseTangramAddressWithoutPrefix()
        {
            AddressParts addressParts = Factory.TryDecodeAddressPartsVerify(TangramAddressWithoutPrefix);

            Assert.IsNotNull(addressParts);
            Assert.AreEqual(WalletAddress, addressParts.Body);
        }

        [Test]
        public void ParseTangramAddressWithWrongPrefixThrowsInvalidAddress()
        {
            Assert.Throws<InvalidAddressException>(() => Factory.DecodeAddressPartsVerifyThrow(TangramAddressWithWrongPrefix, AddressVersion));
        }

        [Test]
        public void ParseTangramAddressWithWrongVersionThrowsInvalidAddress()
        {
            Assert.Throws<InvalidAddressException>(() => Factory.DecodeAddressPartsVerifyThrow(TangramAddressWithWrongVersion, AddressVersion));
        }

        [Test]
        public void ParseTangramAddressWithWrongBodyThrowsInvalidChecksum()
        {
            Assert.Throws<InvalidChecksumException>(() => Factory.DecodeAddressPartsVerifyThrow(TangramAddressWithWrongBody, AddressVersion));
        }

        [Test]
        public void ParseTangramAddressWithWrongChecksumThrowsInvalidChecksum()
        {
            Assert.Throws<InvalidChecksumException>(() => Factory.DecodeAddressPartsVerifyThrow(TangramAddressWithWrongChecksum, AddressVersion));
        }
    }
}
