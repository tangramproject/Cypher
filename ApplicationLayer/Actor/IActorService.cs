using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Actor
{
    public interface IActorService
    {
        event ReceivedMessageEventHandler ReceivedMessage;

        Task<JObject> AddMessageAsync(NotificationDto notification, CancellationToken cancellationToken);
        Task<JObject> AddTokenAsync(TokenDto token, CancellationToken cancellationToken);
        double? Amount();
        ActorService Amount(double? value);
        Span<byte> DecodeAddress(string key);
        EnvelopeDto DeriveEnvelope(SecureString password, int version, double? amount);
        string DeriveKey(int version, string proof, SecureString password, int bytes = 32);
        TokenDto DeriveToken(SecureString password, int version, EnvelopeDto envelope);
        SecureString From();
        ActorService From(SecureString password);
        byte[] GetChiper(string redemptionKey, Span<byte> bobPk);
        Task<NotificationDto> GetMessageAsync(string address, CancellationToken cancellationToken);
        byte[] GetSharedKey(Span<byte> bobPk);
        Task<TokenDto> GetTokenAsync(string stamp, CancellationToken cancellationToken);
        string HotRelease(TokenDto token);
        SecureString Identifier();
        ActorService Identifier(SecureString walletId);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cipher, PkSkDto pkSkDto);
        string PartialRelease(TokenDto token);
        SecureString PublicKey();
        ActorService PublicKey(SecureString pk);
        void ReceivePayment(string redemptionKey);
        SecureString SecretKey();
        ActorService SecretKey(SecureString sk);
        void SendPayment(bool answer);
        Tuple<TokenDto, TokenDto> Swap(SecureString password, int version, string key1, string key2, EnvelopeDto envelope);
        string To();
        ActorService To(string address);
        int VerifyToken(TokenDto terminal, TokenDto current);
    }
}