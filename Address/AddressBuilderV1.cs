using Dawn;
using Sodium;
using System;
using System.Linq;
using System.Text;

namespace Tangram.Address
{
	/// <summary>
	/// * The prefix makes it easy for people to distinguish Tangram addresses, but is optional for parsing.
	/// * The version is encoded as separate text in order to make it easy for address parsers to know whether they should process
	/// an address without decoding the entire text to a byte array (which could cause exceptions).
	/// * The address body and checksum are formatted in base 32, with the Crockford alphabet; they are case insensitive.
	/// * The textual checksum has a fixed size (of "ChecksumCharacterCount" characters) so that parsers don't require a separator
	/// to be able to extract the checksum.
	/// * Textual checksums with a different last character may be valid for the same address body because of how bytes are (not)
	/// aligned to characters.
	/// * There is no major advantage for independently encoding to text the address body and checksum. They could be encoded as
	/// a single text. The only minor advantage is that the textual body could be processed separately, yet still have the same
	/// value as when it's a part of a full Tangram address, so it's visually easy to distinguish it from the checksum.
	/// </summary>
	public abstract class AddressBuilderV1 : AddressBuilder
	{
		public static readonly SimpleBase.Base32Alphabet Base32Alphabet = SimpleBase.Base32Alphabet.Crockford;

		public override string Prefix => "tgm_";
		public override int ChecksumByteCount => 4;

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

			return new NetworkAddress(Version, body);
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

			var toHash = BodySeed.Concat(Version).Concat(sharedData).ToArray();
			var hash = Hash(toHash);

			return hash;
		}

		protected override byte[] BuildChecksum(byte[] body)
		{
			Guard.Argument(body, nameof(body)).MinCount(1);

			var toHash = ChecksumSeed.Concat(Version).Concat(body).ToArray();
			var hash = Hash(toHash);

			var checksum = new byte[ChecksumByteCount];
			Array.Copy(hash, 0, checksum, 0, ChecksumByteCount);

			return checksum;
		}

		protected override bool TryParseNoVerify(string address, out AddressParts parts)
		{
			if (address == null || address.Length < 1 /* version */ + 1 /* body */ + ChecksumCharacterCount)
			{
				parts = null;

				return false;
			}

			string prefix = address.StartsWith(Prefix) ? Prefix : "";
			if (prefix != "" && !string.Equals(prefix, Prefix, StringComparison.InvariantCultureIgnoreCase))
			{
				parts = null;

				return false;
			}

			string version = address.Substring(prefix.Length, 1);
			if (!string.Equals(version, TextualVersion, StringComparison.InvariantCultureIgnoreCase))
			{
				parts = null;

				return false;
			}

			int bodyStartIndex = prefix.Length + version.Length;
			int checksumStartIndex = address.Length - ChecksumCharacterCount;

			string body = address.Substring(bodyStartIndex, checksumStartIndex - bodyStartIndex);
			string checksum = address.Substring(checksumStartIndex, ChecksumCharacterCount);

			var bodyArray = ConvertToArray(body);
			var checksumArray = ConvertToArray(checksum);

			parts = new AddressParts(prefix, TextualVersion, Version, TypedVersion, bodyArray, checksumArray);

			return true;
		}
	}
}
