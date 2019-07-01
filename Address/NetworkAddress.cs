using Dawn;
using System.Linq;

namespace Tangram.Address
{
    public class NetworkAddress : WalletAddress
    {
        public byte[] BinaryVersion { get; } // Mandatory.

        public NetworkAddress(byte[] binaryVersion, byte[] body)
            : base(body)
        {
            Guard.Argument(binaryVersion, nameof(binaryVersion)).NotEmpty();

            BinaryVersion = binaryVersion;
        }

        public NetworkAddress(byte[] binaryVersion, WalletAddress walletAddress)
            : this(binaryVersion, walletAddress.Body)
        {
        }

        public NetworkAddress(AddressParts addressParts)
            : this(addressParts.BinaryVersion, addressParts.Body)
        {
        }

        public override byte[] ToArray()
        {
            return BinaryVersion.Concat(Body).ToArray();
        }
    }
}
