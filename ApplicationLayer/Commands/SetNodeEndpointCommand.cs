using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TangramCypher.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor(new string[] { "setnodeendpoint" }, "Set contact endpoint")]
    public class SetNodeEndpointCommand : Command
    {
        public override async Task Execute()
        {
            throw new NotImplementedException();
        }
    }
}
