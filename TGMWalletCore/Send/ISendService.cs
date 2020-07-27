// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using TGMWalletCore.Actor;

namespace TGMWalletCore.Send
{
    public interface ISendService
    {
        string Graph();
        State State { get; }
        Task Tansfer(Session session);
    }
}
