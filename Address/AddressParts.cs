using Dawn;

namespace Tangram.Address
{
    public class AddressParts
    {
        public AddressVersion Version { get; } // Mandatory.
        public string Prefix { get; } // Optional. Case insensitive.
        public string TextualVersion { get; } // Mandatory.
        public byte[] BinaryVersion { get; } // Mandatory.
        public byte[] Body { get; } // Mandatory.
        public byte[] Checksum { get; } // Mandatory.

        public AddressParts(AddressVersion version, string prefix, string textualVersion, byte[] binaryVersion, byte[] body
            , byte[] checksum)
        {
            Guard.Argument(textualVersion, nameof(textualVersion)).NotEmpty();
            Guard.Argument(binaryVersion, nameof(binaryVersion)).NotEmpty();
            Guard.Argument(body, nameof(body)).NotEmpty();
            Guard.Argument(checksum, nameof(checksum)).NotEmpty();

            Version = version;
            Prefix = prefix;
            TextualVersion = textualVersion;
            BinaryVersion = binaryVersion;
            Body = body;
            Checksum = checksum;
        }
    }
}
