using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "transfer" }, "Transfer funds")]
    public class WalletChiperCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;

        public WalletChiperCommand(IServiceProvider serviceProvider)
        {
            actorService = serviceProvider.GetService<IActorService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            try
            {
                actorService
                    .Identifier(Prompt.GetPassword("Identifier:", ConsoleColor.Yellow).ToSecureString())
                    .From(Prompt.GetPassword("Password:", ConsoleColor.Yellow).ToSecureString())
                    .ReceivePayment(Prompt.GetString("Chiper:", null, ConsoleColor.Green));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Task.CompletedTask;
        }
    }
}
