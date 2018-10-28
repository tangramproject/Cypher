using System.Text;
using Sodium;

namespace TangramCypher.Helpers.LibSodium
{
    public class Cryptography : ICryptography
    {
        public byte[] BoxSeal(string message, byte[] pk)
        {
            var encrypted = SealedPublicKeyBox.Create(Encoding.UTF8.GetBytes(message), pk);
            return encrypted;
        }
        
        public byte[] GenericHash(string message, int bytes = 32)
        {
            var hash = Sodium.GenericHash.Hash(Encoding.UTF8.GetBytes(message), null, bytes);
            return hash;
        }

        public byte[] HashPwd(string pwd)
        {
            const long OPS_LIMIT = 4;
            const int MEM_LIMIT = 33554432;

            var hash = PasswordHash.ArgonHashString(pwd, OPS_LIMIT, MEM_LIMIT);

            return Encoding.UTF8.GetBytes(hash);
        }

        public KeyPairDto KeyPair()
        {
            var kp = Sodium.PublicKeyBox.GenerateKeyPair();
            return new KeyPairDto() { PublicKey = kp.PublicKey, SecretKey = kp.PrivateKey };
        }

        public string OpenBoxSeal(byte[] cipher, Sodium.KeyPair keyPair)
        {
            var decrypted = SealedPublicKeyBox.Open(cipher, keyPair);
            return Encoding.UTF8.GetString(decrypted);
        }

        public byte[] RandomKey()
        {
            return Sodium.GenericHash.GenerateKey();
        }

        public bool VerifiyPwd(byte[] hash, byte[] pwd)
        {
            var isValid = PasswordHash.ArgonHashStringVerify(hash, pwd);
            return isValid;
        }
    }
}