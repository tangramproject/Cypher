using System.Text;
using Sodium;

namespace TangramCypher.Helpers.LibSodium
{
    public class Cryptography : ICryptography
    {
        public byte[] BoxSeal(string message, byte[] pk)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new System.ArgumentException("message", nameof(message));
            }

            if (pk == null)
            {
                throw new System.ArgumentNullException(nameof(pk));
            }

            var encrypted = SealedPublicKeyBox.Create(Encoding.UTF8.GetBytes(message), pk);
            return encrypted;
        }

        public byte[] GenericHash(string message, int bytes = 32)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new System.ArgumentException("Message cannot be null or empty!", nameof(message));
            }

            return Sodium.GenericHash.Hash(Encoding.UTF8.GetBytes(message), null, bytes);
        }

        public byte[] HashPwd(string pwd)
        {
            if (string.IsNullOrEmpty(pwd))
            {
                throw new System.ArgumentException("Password cannot be null or empty!", nameof(pwd));
            }

            const long OPS_LIMIT = 4;
            const int MEM_LIMIT = 33554432;

            var hash = PasswordHash.ArgonHashString(pwd, OPS_LIMIT, MEM_LIMIT);

            return Encoding.UTF8.GetBytes(hash);
        }

        public KeyPairDto KeyPair()
        {
            var kp = PublicKeyBox.GenerateKeyPair();
            return new KeyPairDto() { PublicKey = kp.PublicKey, SecretKey = kp.PrivateKey };
        }

        public string OpenBoxSeal(byte[] cipher, KeyPair keyPair)
        {
            if (cipher == null)
            {
                throw new System.ArgumentNullException(nameof(cipher));
            }

            if (keyPair == null)
            {
                throw new System.ArgumentNullException(nameof(keyPair));
            }

            var decrypted = SealedPublicKeyBox.Open(cipher, keyPair);
            return Encoding.UTF8.GetString(decrypted);
        }

        public byte[] RandomKey()
        {
            return Sodium.GenericHash.GenerateKey();
        }

        public byte[] ScalarMultBase(byte[] sk)
        {
            if (sk == null)
            {
                throw new System.ArgumentNullException(nameof(sk));
            }

            return Sodium.ScalarMult.Base(sk);
        }

        public byte[] ScalarMult(byte[] bobSk, byte[] alicePk)
        {
            if (bobSk == null)
            {
                throw new System.ArgumentNullException(nameof(bobSk));
            }

            if (alicePk == null)
            {
                throw new System.ArgumentNullException(nameof(alicePk));
            }

            return Sodium.ScalarMult.Mult(bobSk, alicePk);
        }

        public bool VerifiyPwd(byte[] hash, byte[] pwd)
        {
            if (hash == null)
            {
                throw new System.ArgumentNullException(nameof(hash));
            }

            if (pwd == null)
            {
                throw new System.ArgumentNullException(nameof(pwd));
            }

            return PasswordHash.ArgonHashStringVerify(hash, pwd);
        }
    }
}