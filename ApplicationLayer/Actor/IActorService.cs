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

        Task<JObject> AddMessageAsync(MessageDto message, CancellationToken cancellationToken);
        Task<JObject> AddCoinAsync(CoinDto coin, CancellationToken cancellationToken);
        double? Amount();
        ActorService Amount(double? value);
        Task<double> BalanceCheck();
        Span<byte> DecodeAddress(string key);
        EnvelopeDto DeriveEnvelope(SecureString password, int version, double? amount);
        string DeriveKey(int version, string proof, SecureString password, int bytes = 32);
        CoinDto DeriveCoin(SecureString password, int version, EnvelopeDto envelope);
        SecureString From();
        ActorService From(SecureString password);
        byte[] GetChiper(string redemptionKey, byte[] pk);
        Task<NotificationDto> GetMessageAsync(string address, CancellationToken cancellationToken);
        byte[] GetSharedKey(byte[] pk);
        Task<CoinDto> GetCoinAsync(string stamp, CancellationToken cancellationToken);
        string HotRelease(CoinDto coin);
        SecureString Identifier();
        ActorService Identifier(SecureString walletId);
        string Memo();
        ActorService Memo(string text);
        string OpenBoxSeal(string cipher, PkSkDto pkSkDto);
        string PartialRelease(CoinDto coin);
        SecureString PublicKey();
        ActorService PublicKey(SecureString pk);
        void ReceivePayment(NotificationDto notification);
        SecureString SecretKey();
        ActorService SecretKey(SecureString sk);
        Task SendPayment(bool answer);
        Tuple<CoinDto, CoinDto> Swap(SecureString password, int version, string key1, string key2, EnvelopeDto envelope);
        string To();
        ActorService To(string address);
        int VerifyCoin(CoinDto terminal, CoinDto current);
    }
}