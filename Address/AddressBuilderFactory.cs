using Dawn;
using System;

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

        public bool Verify(string address, out AddressParts parts)
        {
            // Handle the version according to priority. Handle the testnet version first.

            if (new AddressBuilderV1Testnet().Verify(address, out parts))
                return true;
            else if (new AddressBuilderV1Mainnet().Verify(address, out parts))
                return true;
            else
                return false;
        }

        public bool Verify(AddressParts parts)
        {
            Guard.Argument(parts, nameof(parts)).NotNull();

            var addressBuilder = GetAddressBuilder(parts.Version);

            return addressBuilder != null ? addressBuilder.Verify(parts) : false;
        }

        public AddressParts VerifyThrow(string address, AddressVersion version)
        {
            return GetAddressBuilder(version).VerifyThrow(address);
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
                        throw new ArgumentOutOfRangeException(nameof(version), version, null);
                    else
                        return null;
            }
        }
    }
}
