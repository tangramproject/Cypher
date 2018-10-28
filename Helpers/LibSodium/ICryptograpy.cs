namespace TangramCypher.Helpers.LibSodium
{
    public interface ICryptography
    {
        byte[] BoxSeal(string message, byte[] pk);
        byte[] GenericHash(string message, int bytes = 32);
        byte[] HashPwd(string pwd);
        KeyPairDto KeyPair();
        string OpenBoxSeal(byte[] cipher, Sodium.KeyPair keyPair);
        bool VerifiyPwd(byte[] hash, byte[] pwd);
        byte[] RandomKey();
    }
}