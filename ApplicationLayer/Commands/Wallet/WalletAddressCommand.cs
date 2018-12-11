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
            Console.WriteLine("Mehtod not implemented!");

            return Task.CompletedTask;
        }
    }
}
