using System;
using System.Threading;
using System.Threading.Tasks;
using Cypher.ApplicationLayer.Onion;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        IOnionService _onionService { get; }
        ICryptography _cryptography { get; }

        double? Amount();
        ActorService Amount(double? value);
        string DeriveKey(int version, string proof, string masterKey, int bytes = 32);
        TokenDto DeriveToken(string masterKey, int version, EnvelopeDto envelope);
        Task<TokenDto> FetchToken(string stamp, CancellationToken cancellationToken);
        string From();
        ActorService From(string masterKey);
        string HotRelease(TokenDto token);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cipher, PkSkDto pkSkDto);
        void ReceivePayment(string commitmentKey);
        void SendPayment();
        Tuple<TokenDto, TokenDto> Swap(string masterKey, int version, string key1, string key2, EnvelopeDto envelope);
        string To();
        ActorService To(string address);
        int VerifyToken(TokenDto terminal, TokenDto current);
    }
}