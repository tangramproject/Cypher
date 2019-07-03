using Dawn;
using Sodium;
using System;
using System.Linq;
using System.Text;

namespace Tangram.Address
{
    /// <summary>
    /// V1 addresses are case insensitive.
    /// </summary>
    public abstract class AddressBuilderV1 : AddressBuilder
    {
        public static readonly SimpleBase.Base32Alphabet Base32Alphabet = SimpleBase.Base32Alphabet.Crockford;

        public override string Prefix => "tgm_";
        public override int ChecksumByteCount => 5;

        protected override Encoding TextEncoding => Encoding.UTF8;

        // Cache value.
        private int _ChecksumCharacterCount = 0;
        protected int ChecksumCharacterCount
        {
            get
            {
                if (_ChecksumCharacterCount <= 0)
                {
                    byte[] maxChecksum = new byte[ChecksumByteCount];
                    Array.Fill<byte>(maxChecksum, 255);

                    var checksum = BuildChecksum(maxChecksum);
                    var textualChecksum = ConvertToText(checksum);

                    _ChecksumCharacterCount = textualChecksum.Length;
                }

                return _ChecksumCharacterCount;
            }
        }

        private readonly SimpleBase.Base32 Base32 = new SimpleBase.Base32(Base32Alphabet);

        public override WalletAddress BuildWalletAddress(byte[] sharedData)
        {
            var body = BuildBody(sharedData);

            return new WalletAddress(body);
        }

        public override NetworkAddress BuildNetworkAddress(byte[] sharedData)
        {
            var body = BuildBody(sharedData);

            return new NetworkAddress(BinaryVersion, body);
        }

        public override string EncodeFromBody(byte[] body)
        {
            var checksum = BuildChecksum(body);

            return $"{Prefix}{TextualVersion}{ConvertToText(body)}{ConvertToText(checksum)}";
        }

        protected override byte[] ConvertToArray(string text)
        {
            Guard.Argument(text, nameof(text)).NotEmpty();

            return Base32.Decode(text).ToArray();
        }

        protected override string ConvertToText(byte[] array)
        {
            Guard.Argument(array, nameof(array)).NotEmpty();

            return Base32.Encode(array, false);
        }

        protected override byte[] Hash(byte[] array)
        {
            return CryptoHash.Sha256(array);
        }

        protected override byte[] BuildBody(byte[] sharedData)
        {
            Guard.Argument(sharedData, nameof(sharedData)).MinCount(1);

            var toHash = sharedData;
            var hash = Hash(toHash);

            return hash;
        }

        protected override byte[] BuildChecksum(byte[] body)
        {
            Guard.Argument(body, nameof(body)).MinCount(1);

            var toHash = ChecksumSeed.Concat(BinaryVersion).Concat(body).ToArray();
            var hash = Hash(toHash);

            var checksum = new byte[ChecksumByteCount];
            Array.Copy(hash, 0, checksum, 0, ChecksumByteCount);

            return checksum;
        }

        protected override AddressParts TryDecodeAddressPartsNoVerify(string address)
        {
            if (address == null || address.Length < 1 /* version */ + 1 /* body */ + ChecksumCharacterCount)
                return null;

            string prefix = address.StartsWith(Prefix) ? Prefix : "";
            if (prefix != "" && !string.Equals(prefix, Prefix, StringComparison.InvariantCultureIgnoreCase))
                return null;

            string version = address.Substring(prefix.Length, 1);
            if (!string.Equals(version, TextualVersion, StringComparison.InvariantCultureIgnoreCase))
                return null;

            int bodyStartIndex = prefix.Length + version.Length;
            int checksumStartIndex = address.Length - ChecksumCharacterCount;

            string body = address.Substring(bodyStartIndex, checksumStartIndex - bodyStartIndex);
            string checksum = address.Substring(checksumStartIndex, ChecksumCharacterCount);

            var bodyArray = ConvertToArray(body);
            var checksumArray = ConvertToArray(checksum);

            return new AddressParts(Version, prefix, TextualVersion, BinaryVersion, bodyArray, checksumArray);
        }
    }
}
