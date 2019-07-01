using Dawn;

namespace Tangram.Address
{
	public class WalletAddress
	{
		public byte[] Body { get; } // Mandatory.

		public WalletAddress(byte[] body)
		{
			Guard.Argument(body, nameof(body)).NotEmpty();

			Body = body;
		}

		public WalletAddress(AddressParts addressParts)
			: this(addressParts.Body)
		{
		}

		public virtual byte[] ToArray()
		{
			return Body;
		}
	}
}
