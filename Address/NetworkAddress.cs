using Dawn;
using System.Linq;

namespace Tangram.Address
{
	public class NetworkAddress : WalletAddress
	{
		public byte[] Version { get; } // Mandatory.

		public NetworkAddress(byte[] version, byte[] body)
			: base(body)
		{
			Guard.Argument(version, nameof(version)).NotEmpty();

			Version = version;
		}

		public NetworkAddress(byte[] version, WalletAddress walletAddress)
			: this(version, walletAddress.Body)
		{
		}

		public NetworkAddress(AddressParts addressParts)
			: this(addressParts.Version, addressParts.Body)
		{
		}

		public override byte[] ToArray()
		{
			return Version.Concat(Body).ToArray();
		}
	}
}
