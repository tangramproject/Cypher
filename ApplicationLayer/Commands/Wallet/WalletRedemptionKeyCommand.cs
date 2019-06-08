using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TangramCypher.ApplicationLayer.Actor;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Net.Http;

namespace TangramCypher.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "wallet", "cypher" }, "Claim payment from redemption Key")]
    public class WalletRedemptionKeyCommand : Command
    {
        readonly IActorService actorService;
        readonly IConsole console;
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

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

                    console.ForegroundColor = ConsoleColor.Magenta;
                    console.WriteLine("\nOptions:");
                    console.WriteLine("Local file [1]");
                    console.WriteLine("Web [2]\n");

                    var option = Prompt.GetInt("Select option:", 1, ConsoleColor.Yellow);

                    console.ForegroundColor = ConsoleColor.White;

                    var path = Prompt.GetString("File Path:", null, ConsoleColor.Green);

                    string line = string.Empty;

                    line = option == 1 ? LocalFile(path) : await WebFile(path);

                    var session = new Session(identifier, password) { SenderAddress = address };
                    var message = await actorService.ReceivePaymentRedemptionKey(session, line);

                    console.WriteLine(JsonConvert.SerializeObject(message));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string LocalFile(string path)
        {
            var readLines = File.ReadLines(path).ToArray();
            return readLines[1];
        }

        private async Task<string> WebFile(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                var response = await client.GetAsync(url);
                string xml = await response.Content.ReadAsStringAsync();
                var xmlByteArray = await response.Content.ReadAsByteArrayAsync();
                var path = $"{tangramDirectory}redem{DateTime.Now.GetHashCode()}.rdkey";

                if (!File.Exists(path))
                    File.WriteAllBytes(path, xmlByteArray);

                return LocalFile(path);
            }
        }
    }
}
