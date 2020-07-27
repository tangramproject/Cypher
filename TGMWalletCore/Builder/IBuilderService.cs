// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using TGMWalletCore.Actor;
using TGMWalletCore.Helper;
using TGMWalletCore.Model;

namespace TGMWalletCore.Coin
{
    public interface IBuilderService
    {
        TaskResult<Model.Coin> Build(Session session, Transaction purchase);
    }
}