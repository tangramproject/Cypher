using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helpers.ServiceLocator;
using Microsoft.Extensions.DependencyInjection;
using TangramCypher.ApplicationLayer.Actor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "transfer" }, "Transfer funds")]
    public class WalletTransferCommand : Command
    {
        readonly IActorService actorService;
        readonly IVaultService vaultService;
        readonly IConsole console;

        public WalletTransferCommand()
        {
            var serviceProvider = Locator.Instance.GetService<IServiceProvider>();

            actorService = serviceProvider.GetService<IActorService>();
            vaultService = serviceProvider.GetService<IVaultService>();
            console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            var identifier = Prompt.GetPassword("Identifier:", ConsoleColor.Yellow);
            var password = Prompt.GetPassword("Password:", ConsoleColor.Yellow);
            var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
            var address = Prompt.GetString("To:", null, ConsoleColor.Green);

            var data = await vaultService.GetDataAsync(identifier, password, $"wallets/{identifier}/wallet");
            var stroreKeys = JObject.FromObject(data.Data["storeKeys"]);

            actorService
                .From(password)
                .Secret(stroreKeys.GetValue("SecretKey").Value<string>())
                .Amount(Convert.ToDouble(amount))
                .To(address)
                .SendPayment();
        }
    }
}
