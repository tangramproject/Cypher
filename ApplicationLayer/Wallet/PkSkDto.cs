using System.Security;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public class PkSkDto
    {
        public string PublicKey;
        public SecureString SecretKey;
        public string Address;
    }
}

