using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "transfer" }, "Transfer funds")]
    public class WalletTransferCommand : Command
    {
        public override Task Execute()
        {
            throw new NotImplementedException();
        }
    }
}
