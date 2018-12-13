using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
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


        public WalletBalanceCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            cryptography = serviceProvider.GetService<ICryptography>();
            walletService = serviceProvider.GetService<IWalletService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            try
            {
                actorService.Identifier(Prompt.GetPassword("Identifier:", ConsoleColor.Yellow).ToSecureString());
                actorService.From(Prompt.GetPassword("Password:", ConsoleColor.Yellow).ToSecureString());

                var securePk = await walletService.GetStoreKey(actorService.Identifier(), actorService.From(), "PublicKey");

                using (var insecurePk = securePk.Insecure())
                {
                    var pk = Utilities.HexToBinary(insecurePk.Value);
                    var sharedKey = actorService.GetSharedKey(pk);
                    var notificationAddress = cryptography.GenericHashWithKey(Utilities.BinaryToHex(pk.ToArray()), sharedKey);

                    try
                    {
                        var message = await actorService.GetMessageAsync(Utilities.BinaryToHex(notificationAddress), new CancellationToken());

                        using (var insecureSk = actorService.SecretKey().Insecure())
                        {
                            var redemptionKey = cryptography.OpenBoxSeal(Convert.FromBase64String(message.Chiper), new KeyPair(pk.ToArray(), Utilities.HexToBinary(insecureSk.Value)));

                            actorService.ReceivePayment(redemptionKey);
                        }
                    }
                    catch (Exception e)
                    {

                    }
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
