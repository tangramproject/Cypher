﻿// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helper;
using TangramCypher.ApplicationLayer.Wallet;
using Kurukuru;
using Microsoft.Extensions.Logging;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "receive" }, "Receive payment")]
    public class WalletReceivePaymentCommand : Command
    {
        readonly IActorService actorService;
        readonly IWalletService walletService;
        readonly ILogger logger;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            walletService = serviceProvider.GetService<IWalletService>();
            logger = serviceProvider.GetService<ILogger>();

            actorService.MessagePump += ActorService_MessagePump;
        }

        public override async Task Execute()
        {

            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow);

            var address = Prompt.GetString("Address:", null, ConsoleColor.Red);

            if (!string.IsNullOrEmpty(address))
            {
                await Spinner.StartAsync("Processing receive payment(s) ...", async spinner =>
                {
                    this.spinner = spinner;

                    await Task.Delay(500);

                    try
                    {
                        var session = new Session(identifier, password) { SenderAddress = address };
                        await actorService.ReceivePayment(session);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        throw ex;
                    }
                    finally
                    {
                        var transaction = walletService.LastTransaction(identifier, password, TransactionType.Receive);
                        var txnReceivedAmount = transaction == null ? 0.ToString() : transaction.Amount.DivWithNaT().ToString("F9");
                        var txnMemo = transaction == null ? "" : transaction.Memo;
                        var balance = walletService.AvailableBalance(identifier, password);

                        spinner.Text = $"Memo: {txnMemo}  Received: {txnReceivedAmount}  Available Balance: {balance.Result.DivWithNaT().ToString("F9")}";
                    }
                }, Patterns.Toggle3);
            }
        }

        private void ActorService_MessagePump(object sender, MessagePumpEventArgs e)
        {
            spinner.Text = e.Message;
        }
    }
}

