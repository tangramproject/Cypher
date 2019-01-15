namespace TangramCypher.Helpers.LibSodium
{
    public interface ICryptography
    {
        byte[] BoxSeal(string message, byte[] pk);
        byte[] GenericHashNoKey(string message, int bytes = 32);
        byte[] GenericHashWithKey(string message, byte[] key, int bytes = 32);
        byte[] HashPwd(string pwd);
        KeyPairDto KeyPair();
        string OpenBoxSeal(byte[] cipher, Sodium.KeyPair keyPair);
        bool VerifiyPwd(byte[] hash, byte[] pwd);
        byte[] RandomKey();
        byte[] ShortHash(string message, byte[] key);
        int RandomNumbers(int n);
        byte[] RandomBytes(int bytes = 32);
        byte[] ScalarBase(byte[] sk);
        byte[] ScalarMult(byte[] sk, byte[] pk);
    }
}