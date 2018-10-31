using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "balance" }, "Get current wallet balance")]
    public class WalletBalanceCommand : Command
    {
        public WalletBalanceCommand()
        {
        }

        public override Task Execute()
        {
            throw new NotImplementedException();
        }
    }
}
