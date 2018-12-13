using System.Security;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        Task<double> GetBalance(SecureString identifier, SecureString password);
        SecureString Id { get; set; }
        PkSkDto CreatePkSk();
        SecureString NewID(int bytes = 32);
        SecureString Passphrase();
        Task AddEnvelope(SecureString identifier, SecureString password, EnvelopeDto envelope);
        Task<SecureString> GetStoreKey(SecureString identifier, SecureString password, string storeKey);
    }
}