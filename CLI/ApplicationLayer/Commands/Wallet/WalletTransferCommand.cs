// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.IO;
using Kurukuru;
using Microsoft.Extensions.Logging;
using NBitcoin;
using TGMWalletCore.Coin;
using TGMWalletCore.Actor;
using TGMWalletCore.Wallet;
using TGMWalletCore.Send;
using TGMWalletCore.Helper;
using TGMWalletCore.Model;

namespace Tangram.Bamboo.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "send" }, "Send funds")]
    public class WalletTransferCommand : Command
    {
        private readonly IBuilderService _builderService;
        private readonly IActorService _actorService;
        private readonly IWalletService _walletService;
        private readonly ISendService _sendService;
        private readonly IConsole _console;
        private readonly ILogger _logger;

        private Spinner spinner;

        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        public WalletTransferCommand(IServiceProvider serviceProvider)
        {
            _builderService = serviceProvider.GetService<IBuilderService>();
            _actorService = serviceProvider.GetService<IActorService>();
            _walletService = serviceProvider.GetService<IWalletService>();
            _sendService = serviceProvider.GetService<ISendService>();
            _console = serviceProvider.GetService<IConsole>();
            _logger = serviceProvider.GetService<ILogger>();
        }

        public override async Task Execute()
        {
            _actorService.MessagePump += ActorService_MessagePump;

            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var address = Prompt.GetString("To:", null, ConsoleColor.Red);
            var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
            var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
            var yesNo = Prompt.GetYesNo("Send redemption key to message pool?", true, ConsoleColor.Yellow);

            if (double.TryParse(amount, out double t))
            {
                await Spinner.StartAsync("Processing payment ...", async spinner =>
                {
                    this.spinner = spinner;

                    try
                    {
                        var session = new Session(identifier, passphrase)
                        {
                            Amount = t.ConvertToUInt64(),
                            Memo = memo,
                            RecipientAddress = address
                        };

                        session = _actorService.SessionAddOrUpdate(session);

                        _actorService.Unlock(session.SessionId);

                        var tx = new TGMWalletCore.Model.Transaction { Balance = 183_744_990d.ConvertToUInt64(), Input = 183_744_940d.ConvertToUInt64(), Output = 50d.ConvertToUInt64(), EphemKey = new Key().PubKey.ToHex() };

                        var coin = _builderService.Build(session, tx);

                        var sendResult = await _actorService.PostArticle(coin.Result, RestApiMethod.PostCoin);

                        tx.Address = session.SenderAddress;
                        tx.TransactionId = session.SessionId;
                        tx.TransactionType = TransactionType.Receive;
                         
                        _actorService.SaveTransaction(session.SessionId, tx);


                        await _sendService.Tansfer(session);
                        session = _actorService.GetSession(session.SessionId);

                        if (_sendService.State != State.Completed)
                        {
                            var failedMessage = JsonConvert.SerializeObject(session.LastError.GetValue("message"));
                            _logger.LogCritical(failedMessage);
                            spinner.Fail(failedMessage);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        throw ex;
                    }
                    finally
                    {
                        var balance = _walletService.AvailableBalance(identifier, passphrase);

                        spinner.Text = $"Available Balance: {balance.Result.DivWithNaT().ToString("F9")}";
                        _actorService.MessagePump -= ActorService_MessagePump;
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
