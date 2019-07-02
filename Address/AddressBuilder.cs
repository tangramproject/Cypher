using Dawn;
using Sodium;
using System;
using System.Linq;
using System.Text;
using Tangram.Address.Exceptions;

namespace Tangram.Address
{
    /// <summary>
    /// The textual seeds are converted to byte arrays and hashed in order to improve the performance when they are later hashed
    /// together with the address body and checksum.
    /// </summary>
    public abstract class AddressBuilder
    {
        public abstract AddressVersion Version { get; }
        public abstract string Prefix { get; } // Optional. Case insensitive.
        public abstract string TextualVersion { get; } // Mandatory. The testnet can have the same version forever.
        public abstract byte[] BinaryVersion { get; } // Mandatory. The testnet can have the same version forever.
        public abstract int ChecksumByteCount { get; }
        public abstract string TextualBodySeed { get; } // Mandatory. Should be unique for every version.
        public abstract string TextualChecksumSeed { get; } // Mandatory. Should be unique for every version.

        protected abstract Encoding TextEncoding { get; }

        // Cache value.
        private byte[] _BodySeed;
        protected byte[] BodySeed
        {
            get
            {
                if (_BodySeed == null)
                    _BodySeed = Hash(TextEncoding.GetBytes(TextualBodySeed));

                return _BodySeed;
            }
        }

        // Cache value.
        private byte[] _ChecksumSeed;
        protected byte[] ChecksumSeed
        {
            get
            {
                if (_ChecksumSeed == null)
                    _ChecksumSeed = Hash(TextEncoding.GetBytes(TextualChecksumSeed));

                return _ChecksumSeed;
            }
        }

        public string EncodeFromSharedData(byte[] sharedData)
        {
            var body = BuildBody(sharedData);

            return EncodeFromBody(body);
        }

        public string Encode(WalletAddress walletAddress)
        {
            Guard.Argument(walletAddress, nameof(walletAddress)).NotNull();

            return EncodeFromBody(walletAddress.Body);
        }

        public string Encode(NetworkAddress networkAddress)
        {
            Guard.Argument(networkAddress, nameof(networkAddress)).NotNull();

            if (!BinaryVersion.SequenceEqual(networkAddress.BinaryVersion))
            {
                throw new InvalidAddressException($"The network address version '{Utilities.BinaryToHex(networkAddress.BinaryVersion)}'"
                    + $" is different than the address builder version '{Utilities.BinaryToHex(BinaryVersion)}'");
            }

            return EncodeFromBody(networkAddress.Body);
        }

        public bool Verify(string address, out AddressParts parts)
        {
            if (!TryParseNoVerify(address, out parts))
                return false;

            return Verify(parts);
        }

        public bool Verify(AddressParts parts)
        {
            Guard.Argument(parts, nameof(parts)).NotNull();

            if (!BinaryVersion.SequenceEqual(parts.BinaryVersion))
                return false;

            var checksum = BuildChecksum(parts.Body);

            return checksum.SequenceEqual(parts.Checksum);
        }

        public AddressParts VerifyThrow(string address)
        {
            if (!TryParseNoVerify(address, out AddressParts parts))
                throw new InvalidAddressException($"Failed to parse wallet address '{address}'.");

            if (!BinaryVersion.SequenceEqual(parts.BinaryVersion))
            {
                throw new InvalidAddressException($"The address version '{Utilities.BinaryToHex(parts.BinaryVersion)}'"
                    + $" is different than the address builder version '{Utilities.BinaryToHex(BinaryVersion)}'");
            }

            var checksum = BuildChecksum(parts.Body);

            if (!checksum.SequenceEqual(parts.Checksum))
                throw new InvalidChecksumException($"Invalid checksum for wallet address '{address}'.");

            return parts;
        }

        public abstract WalletAddress BuildWalletAddress(byte[] sharedData);
        public abstract NetworkAddress BuildNetworkAddress(byte[] sharedData);
        public abstract string EncodeFromBody(byte[] body);

        protected abstract byte[] ConvertToArray(string text);
        protected abstract string ConvertToText(byte[] array);
        protected abstract byte[] Hash(byte[] array);
        protected abstract byte[] BuildBody(byte[] sharedData);
        protected abstract byte[] BuildChecksum(byte[] body);
        protected abstract bool TryParseNoVerify(string address, out AddressParts parts);
    }
}
