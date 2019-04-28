using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Actor;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Newtonsoft.Json;

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
                    var address = Prompt.GetString("Address:", null, ConsoleColor.Red);
                    var path = Prompt.GetString("File Path:", null, ConsoleColor.Green);

                    var readLines = File.ReadLines(path).ToArray();
                    var line = readLines[1];

                    var message = await actorService
                        .MasterKey(password)
                        .Identifier(identifier)
                        .ReceivePaymentRedemptionKey(address, line);

                    console.WriteLine(JsonConvert.SerializeObject(message));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
