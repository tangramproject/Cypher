using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Wallet
{
    public interface IWalletService
    {
        ICollection<PkSkDto> Store { get; set; }
        string Id { get; set; }

        PkSkDto CreatePkSk();

        string NewID();

        string MasterKey();

        string Passphrase(int listOfWords);
    }
}