using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Actor;
using Microsoft.Extensions.DependencyInjection;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "cypher" }, "Claim payment from redemption Key")]
    public class WalletRedemptionKeyCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;

        public WalletRedemptionKeyCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            try
            {
                using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
                using (var password = Prompt.GetPasswordAsSecureString("Password:", ConsoleColor.Yellow))
                {
                    var cypher = Prompt.GetString("Redemption Key:", null, ConsoleColor.Green);
                    var address = Prompt.GetString("Address:", null, ConsoleColor.Red);

                    var message = await actorService
                        .From(password)
                        .Identifier(identifier)
                        .ReceivePaymentRedemptionKey(address, cypher);

                    console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
