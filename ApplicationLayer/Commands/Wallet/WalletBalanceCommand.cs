using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sodium;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "balance" }, "Get current wallet balance")]
    public class WalletBalanceCommand : Command
    {
        readonly IActorService actorService;
        readonly ICryptography cryptography;
        readonly IWalletService walletService;
        readonly IConsole console;
        readonly ILogger logger;


        public WalletBalanceCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            cryptography = serviceProvider.GetService<ICryptography>();
            walletService = serviceProvider.GetService<IWalletService>();
            console = serviceProvider.GetService<IConsole>();
            logger = serviceProvider.GetService<ILogger>();
        }

        public override async Task Execute()
        {
            try
            {
                var identifier = Prompt.GetPassword("Identifier:", ConsoleColor.Yellow).ToSecureString();
                var password = Prompt.GetPassword("Password:", ConsoleColor.Yellow).ToSecureString();
                var storePk = await walletService.GetStoreKey(identifier, password, "PublicKey");
                var pk = Utilities.HexToBinary(storePk.ToUnSecureString());

                // var sharedKey = actorService.GetSharedKey(pk);
                // TODO: Needs reworking..
                var notificationAddress = cryptography.GenericHashWithKey(pk.ToHex(), pk).ToHex();
                try
                {
                    var message = await actorService.GetMessageAsync(notificationAddress, new CancellationToken());

                    actorService
                        .Identifier(identifier)
                        .From(password)
                        .ReceivePayment(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }

                var total = await walletService.GetBalance(actorService.Identifier(), actorService.From());

                console.WriteLine($"Wallet balance: {total}");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
