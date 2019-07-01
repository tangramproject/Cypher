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
			var walletAddress = Factory.BuildWalletAddress(PublicKey, AddressVersion);

			Trace.WriteLine($"Address version = '{AddressVersion}'"
				+ $", wallet address = '{SimpleBase.Base16.EncodeUpper(walletAddress.ToArray())}'");

			Assert.AreEqual(WalletAddress, walletAddress.ToArray());
		}

		[Test]
		public void BuildNetworkAddress()
		{
			var networkAddress = Factory.BuildNetworkAddress(PublicKey, AddressVersion);

			Trace.WriteLine($"Address version = '{AddressVersion}'"
				+ $", network address = '{SimpleBase.Base16.EncodeUpper(networkAddress.ToArray())}'");

			Assert.AreEqual(NetworkAddress, networkAddress.ToArray());
		}

		[Test]
		public void EncodeTangramAddress()
		{
			var walletAddress = Factory.BuildWalletAddress(PublicKey, AddressVersion);
			var tangramAddress = Factory.Encode(walletAddress, AddressVersion);

			Trace.WriteLine($"Address version = '{AddressVersion}', tangram address = '{tangramAddress}'");

			Assert.AreEqual(TangramAddress, tangramAddress);
		}

		[Test]
		public void ParseTangramAddress()
		{
			bool verified = Factory.Verify(TangramAddress, out AddressParts addressParts);

			Assert.IsTrue(verified);
			Assert.AreEqual(WalletAddress, addressParts.Body);
		}

		[Test]
		public void ParseLowerCaseTangramAddress()
		{
			bool verified = Factory.Verify(TangramAddress.ToLowerInvariant(), out AddressParts addressParts);

			Assert.IsTrue(verified);
			Assert.AreEqual(WalletAddress, addressParts.Body);
		}

		[Test]
		public void ParseTangramAddressWithoutPrefix()
		{
			bool verified = Factory.Verify(TangramAddressWithoutPrefix, out AddressParts addressParts);

			Assert.IsTrue(verified);
			Assert.AreEqual(WalletAddress, addressParts.Body);
		}

		[Test]
		public void ParseTangramAddressWithWrongPrefixThrowsInvalidAddress()
		{
			Assert.Throws<InvalidAddressException>(() => Factory.VerifyThrow(TangramAddressWithWrongPrefix, AddressVersion));
		}

		[Test]
		public void ParseTangramAddressWithWrongVersionThrowsInvalidAddress()
		{
			Assert.Throws<InvalidAddressException>(() => Factory.VerifyThrow(TangramAddressWithWrongVersion, AddressVersion));
		}

		[Test]
		public void ParseTangramAddressWithWrongBodyThrowsInvalidChecksum()
		{
			Assert.Throws<InvalidChecksumException>(() => Factory.VerifyThrow(TangramAddressWithWrongBody, AddressVersion));
		}

		[Test]
		public void ParseTangramAddressWithWrongChecksumThrowsInvalidChecksum()
		{
			Assert.Throws<InvalidChecksumException>(() => Factory.VerifyThrow(TangramAddressWithWrongChecksum, AddressVersion));
		}
	}
}
