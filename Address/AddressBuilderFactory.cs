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

        public string EncodeFromSharedData(byte[] sharedData, AddressVersion version)
        {
            return GetAddressBuilder(version).EncodeFromSharedData(sharedData);
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

        public WalletAddress TryDecodeWalletAddressVerify(string address)
        {
            AddressParts addressParts = TryDecodeAddressPartsVerify(address);

            return addressParts != null ? new WalletAddress(addressParts) : null;
        }

        public NetworkAddress TryDecodeNetworkAddressVerify(string address)
        {
            AddressParts addressParts = TryDecodeAddressPartsVerify(address);

            return addressParts != null ? new NetworkAddress(addressParts) : null;
        }

        public AddressParts TryDecodeAddressPartsVerify(string address, AddressVersion version)
        {
            var addressBuilder = GetAddressBuilder(version, false);

            return addressBuilder != null ? addressBuilder.TryDecodeAddressPartsVerify(address) : null;
        }

        public WalletAddress TryDecodeWalletAddressVerify(string address, AddressVersion version)
        {
            var addressBuilder = GetAddressBuilder(version, false);

            return addressBuilder != null ? addressBuilder.TryDecodeWalletAddressVerify(address) : null;
        }

        public NetworkAddress TryDecodeNetworkAddressVerify(string address, AddressVersion version)
        {
            var addressBuilder = GetAddressBuilder(version, false);

            return addressBuilder != null ? addressBuilder.TryDecodeNetworkAddressVerify(address) : null;
        }

        public AddressParts DecodeAddressPartsVerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).DecodeAddressPartsVerifyThrow(address);
        }

        public WalletAddress DecodeWalletAddressVerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).DecodeWalletAddressVerifyThrow(address);
        }

        public NetworkAddress DecodeNetworkAddressVerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).DecodeNetworkAddressVerifyThrow(address);
        }

        public WalletAddress BuildWalletAddress(byte[] sharedData, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildWalletAddress(sharedData);
        }

        public NetworkAddress BuildNetworkAddress(byte[] sharedData, AddressVersion version)
        {
            return GetAddressBuilder(version).BuildNetworkAddress(sharedData);
        }

        public string EncodeFromBody(byte[] body, AddressVersion version)
        {
            return GetAddressBuilder(version).EncodeFromBody(body);
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
