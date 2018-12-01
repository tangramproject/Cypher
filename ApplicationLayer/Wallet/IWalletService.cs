using System.Collections.Generic;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        ICryptography _Cryptography { get; }
        string Id { get; set; }
        ICollection<PkSkDto> Store { get; set; }
        PkSkDto CreatePkSk();
        string NewID(int bytes = 32);
        string MasterKey();
        string Passphrase();
    }
}