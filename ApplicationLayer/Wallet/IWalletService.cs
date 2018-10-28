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
        string NewID();
        string MasterKey();
        string Passphrase(int listOfWords);
    }
}