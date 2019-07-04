using Dawn;
using Tangram.Address.Exceptions;

namespace Tangram.Address
{
    public class AddressBuilderFactory
    {
        private readonly static object GlobalLock = new object();
        public static AddressBuilderFactory _Global;
        public static AddressBuilderFactory Global
        {
            get
            {
                lock (GlobalLock)
                {
                    if (_Global == null)
                        _Global = new AddressBuilderFactory();

                    return Global;
                }
            }
        }

        public byte[] BuildBodyFromExactData(byte[] body, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildBodyFromExactData(body);
        }

        public byte[] BuildBodyFromPublicKey(byte[] publicKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildBodyFromPublicKey(publicKey);
        }

        public byte[] BuildBodyFromSharedBlob(byte[] sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildBodyFromSharedBlob(sharedBlob, compressionKey);
        }

        public byte[] BuildBodyFromSharedBlob(string sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildBodyFromSharedBlob(sharedBlob, compressionKey);
        }

        public WalletAddress BuildWalletAddressFromBody(byte[] body, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildWalletAddressFromBody(body);
        }

        public WalletAddress BuildWalletAddressFromPublicKey(byte[] publicKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildWalletAddressFromPublicKey(publicKey);
        }

        public WalletAddress BuildWalletAddressFromSharedBlob(byte[] sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildWalletAddressFromSharedBlob(sharedBlob, compressionKey);
        }

        public WalletAddress BuildWalletAddressFromSharedBlob(string sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildWalletAddressFromSharedBlob(sharedBlob, compressionKey);
        }

        public NetworkAddress BuildNetworkAddressFromBody(byte[] body, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildNetworkAddressFromBody(body);
        }

        public NetworkAddress BuildNetworkAddressFromPublicKey(byte[] publicKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildNetworkAddressFromPublicKey(publicKey);
        }

        public NetworkAddress BuildNetworkAddressFromSharedBlob(byte[] sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildNetworkAddressFromSharedBlob(sharedBlob, compressionKey);
        }

        public NetworkAddress BuildNetworkAddressFromSharedBlob(string sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildNetworkAddressFromSharedBlob(sharedBlob, compressionKey);
        }

        public string EncodeFromBody(byte[] body, AddressVersion version)
        {
            return GetAddressBuilder(version).EncodeFromBody(body);
        }

        public string EncodeFromPublicKey(byte[] publicKey, AddressVersion version)
        {
            return GetAddressBuilder(version).EncodeFromPublicKey(publicKey);
        }

        public string EncodeFromSharedBlob(byte[] sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).EncodeFromSharedBlob(sharedBlob, compressionKey);
        }

        public string EncodeFromSharedBlob(string sharedBlob, byte[] compressionKey, AddressVersion version)
        {
            return GetAddressBuilder(version).EncodeFromSharedBlob(sharedBlob, compressionKey);
        }

        public string Encode(WalletAddress walletAddress, AddressVersion version)
        {
            return GetAddressBuilder(version).Encode(walletAddress);
        }

        public string Encode(NetworkAddress networkAddress, AddressVersion version)
        {
            return GetAddressBuilder(version).Encode(networkAddress);
        }

        public bool TryVerify(AddressParts parts)
        {
            Guard.Argument(parts, nameof(parts)).NotNull();

            var addressBuilder = GetAddressBuilder(parts.Version, false);

            return addressBuilder != null ? addressBuilder.TryVerify(parts) : false;
        }

        public AddressParts TryDecodeAddressPartsVerify(string address)
        {
            // Handle the version according to priority. Handle the testnet version first.

            AddressParts addressParts;

            addressParts = new AddressBuilderV1Testnet().TryDecodeAddressPartsVerify(address);
            if (addressParts != null)
                return addressParts;

            addressParts = new AddressBuilderV1Mainnet().TryDecodeAddressPartsVerify(address);
            if (addressParts != null)
                return addressParts;

            return null;
        }

        public AddressParts TryDecodeAddressPartsVerify(string address, AddressVersion version)
        {
            var addressBuilder = GetAddressBuilder(version, false);

            return addressBuilder != null ? addressBuilder.TryDecodeAddressPartsVerify(address) : null;
        }

        public WalletAddress TryDecodeWalletAddressVerify(string address)
        {
            AddressParts addressParts = TryDecodeAddressPartsVerify(address);

            return addressParts != null ? new WalletAddress(addressParts) : null;
        }

        public WalletAddress TryDecodeWalletAddressVerify(string address, AddressVersion version)
        {
            var addressBuilder = GetAddressBuilder(version, false);

            return addressBuilder != null ? addressBuilder.TryDecodeWalletAddressVerify(address) : null;
        }

        public NetworkAddress TryDecodeNetworkAddressVerify(string address)
        {
            AddressParts addressParts = TryDecodeAddressPartsVerify(address);

            return addressParts != null ? new NetworkAddress(addressParts) : null;
        }

        public NetworkAddress TryDecodeNetworkAddressVerify(string address, AddressVersion version)
        {
            var addressBuilder = GetAddressBuilder(version, false);

            return addressBuilder != null ? addressBuilder.TryDecodeNetworkAddressVerify(address) : null;
        }

        public AddressParts DecodeAddressPartsVerifyThrow(string address)
        {
            AddressParts addressParts = TryDecodeAddressPartsVerify(address);

            if (addressParts == null)
                throw new InvalidAddressException($"Failed to decode address '{address}'.");

            return addressParts;
        }

        public AddressParts DecodeAddressPartsVerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).DecodeAddressPartsVerifyThrow(address);
        }

        public WalletAddress DecodeWalletAddressVerifyThrow(string address)
        {
            AddressParts addressParts = DecodeAddressPartsVerifyThrow(address);

            return new WalletAddress(addressParts);
        }

        public WalletAddress DecodeWalletAddressVerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).DecodeWalletAddressVerifyThrow(address);
        }

        public NetworkAddress DecodeNetworkAddressVerifyThrow(string address)
        {
            AddressParts addressParts = DecodeAddressPartsVerifyThrow(address);

            return new NetworkAddress(addressParts);
        }

        public NetworkAddress DecodeNetworkAddressVerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).DecodeNetworkAddressVerifyThrow(address);
        }

        private AddressBuilder GetAddressBuilder(AddressVersion version, bool throwIfUnknownVersion = true)
        {
            // Handle the version according to priority. Handle the testnet version first.

            switch (version)
            {
                case AddressVersion.V1Testnet:
                    return new AddressBuilderV1Testnet();
                case AddressVersion.V1Mainnet:
                    return new AddressBuilderV1Mainnet();
                default:
                    if (throwIfUnknownVersion)
                        throw new UnknownAddressVersionException($"Unknown address version '{version}'.");
                    else
                        return null;
            }
        }
    }
}
