using System;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        ICryptography _Cryptography { get; }

        double? Amount();
        ActorService Amount(double? value);
        string DeriveKey(int version, string proof, string masterKey, int bytes = 32);
        ChronicleDto DeriveToken(string masterKey, int version, ProofTokenDto proofTokenDto);
        string From();
        ActorService From(string masterKey);
        string HotRelease(ChronicleDto chronicleDto);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cipher, PkSkDto pkSkDto);
        void ReceivePayment(string commitmentKey);
        void SendPayment();
        Tuple<ChronicleDto, ChronicleDto> Swap(string masterKey, int version, string key1, string key2, ProofTokenDto proofTokenDto);
        string To();
        ActorService To(string address);
        int VerifyToken(ChronicleDto terminal, ChronicleDto current);
    }
}