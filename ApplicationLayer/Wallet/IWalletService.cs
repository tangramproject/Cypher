using System.Collections.Generic;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        ICryptography _cryptography { get; }
        string _id { get; set; }
        ICollection<PkSkDto> _store { get; set; }
        PkSkDto CreatePkSk();
        string NewID(int bytes = 32);
        string MasterKey();
        string Passphrase();
    }
}