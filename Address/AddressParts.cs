using Dawn;

namespace Tangram.Address
{
	public class AddressParts
	{
		public string Prefix { get; } // Optional. Case insensitive.
		public string TextualVersion { get; } // Mandatory. Case insensitive.
		public byte[] Version { get; } // Mandatory.
		public AddressVersion TypedVersion { get; } // Mandatory.
		public byte[] Body { get; } // Mandatory.
		public byte[] Checksum { get; } // Mandatory.

		public AddressParts(string prefix, string textualVersion, byte[] version, AddressVersion typedVersion, byte[] body
			, byte[] checksum)
		{
			Guard.Argument(textualVersion, nameof(textualVersion)).NotEmpty();
			Guard.Argument(version, nameof(version)).NotEmpty();
			Guard.Argument(body, nameof(body)).NotEmpty();
			Guard.Argument(checksum, nameof(checksum)).NotEmpty();

			Prefix = prefix;
			TextualVersion = textualVersion;
			Version = version;
			TypedVersion = typedVersion;
			Body = body;
			Checksum = checksum;
		}
	}
}
