using System;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    public class WalletAddressCommand : Command
    {
        public WalletAddressCommand()
        {
        }

        public override Task Execute()
        {
            Console.WriteLine("Method not implemented!");

            return Task.CompletedTask;
        }
    }
}
