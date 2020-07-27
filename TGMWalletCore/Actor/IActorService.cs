// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TGMWalletCore.Helper;
using TGMWalletCore.Helper.Http;

namespace TGMWalletCore.Actor
{
    public interface IActorService
    {
        event MessagePumpEventHandler MessagePump;

        Session GetSession(Guid sessionId);
        Client GetClient(IConfiguration configuration);

        Task ReceivePayment(Session session);
        Session SessionAddOrUpdate(Session session);
        TaskResult<Session> SufficientFunds(Guid sessionId);
        TaskResult<bool> Unlock(Guid sessionId);
        TaskResult<bool> Spend(Guid sessionId);
        Task<TaskResult<byte[]>> PostArticle<T>(T payload, RestApiMethod api) where T : class;
        void UpdateMessagePump(string message);
        bool SaveTransaction(Guid sessionId, Model.Transaction transaction);
    }
}