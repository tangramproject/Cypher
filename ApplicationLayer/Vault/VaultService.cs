using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault.Models;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SystemBackend;

namespace TangramCypher.ApplicationLayer.Vault
{
    public class VaultService : IVaultService
    {
        private static readonly DirectoryInfo userDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(Path.Combine(userDirectory.FullName, ".tangramcli"));

        private FileInfo vaultExecutable;
        private Process vaultProcess;
        private IVaultClient vaultClient;

        private const int SECRET_SHARES = 5;
        private const int SECRET_THRESHOLD = 2;
        private const string VAULT_ENDPOINT = "http://127.0.0.1:8200";

        public VaultService()
        {
            var vaultClientSettings = new VaultClientSettings(VAULT_ENDPOINT, null);
            //  TODO: Pull this from settings file.
            vaultClient = new VaultClient(vaultClientSettings);
        }

        public async Task StartVaultServiceAsync()
        {
            //  Find Vault Executable
            FileInfo[] fileInfo = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileInfo = tangramDirectory.GetFiles("vault.exe", SearchOption.TopDirectoryOnly);
            }
            else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                fileInfo = tangramDirectory.GetFiles("vault", SearchOption.TopDirectoryOnly);
            }

            if (fileInfo != null && fileInfo.Length == 1)
            {
                vaultExecutable = fileInfo[0];
            }

            //  Launch service
            PhysicalConsole.Singleton.ResetColor();
            PhysicalConsole.Singleton.WriteLine("Starting Vault Service.");

            var vaultProcesses = Process.GetProcessesByName("vault");

            if (vaultProcesses.Length == 1)
            {
                vaultProcess = vaultProcesses[0];

                Console.ResetColor();
                PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.Yellow;
                PhysicalConsole.Singleton.WriteLine("Warning: Existing Vault Process Detected.");
                PhysicalConsole.Singleton.WriteLine("Please be sure to type `exit` to close the wallet properly.");
            }
            else
            {
                vaultProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = vaultExecutable.FullName,
                    Arguments = "server -config vault.json",
                    WorkingDirectory = tangramDirectory.FullName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                while (!vaultProcess.StandardOutput.EndOfStream)
                {
                    string line = vaultProcess.StandardOutput.ReadLine();

                    if (line.Contains("Vault server started!"))
                    {
                        PhysicalConsole.Singleton.ResetColor();
                        PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.DarkGreen;
                        PhysicalConsole.Singleton.WriteLine("Vault Server Started!");
                        break;
                    }
                }
            }

            var initStatus = await vaultClient.V1.System.GetInitStatusAsync();

            if (!initStatus)
            {
                await Init();
            }
        }

        public async Task Unseal(string shard)
        {
            var unsealTask = await vaultClient.V1.System.UnsealAsync(shard);

            var response = unsealTask;

            if (!response.Sealed)
            {
                PhysicalConsole.Singleton.ResetColor();
                PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.DarkGreen;
                PhysicalConsole.Singleton.WriteLine("Vault Unsealed!");
            }
        }

        public async Task Seal()
        {
            await vaultClient.V1.System.SealAsync();
        }

        public async Task RevokeToken(string token)
        {
            PhysicalConsole.Singleton.WriteLine("Revoking Root Token");
            await vaultClient.V1.System.RevokeLeaseAsync(token);
        }

        public async Task Init()
        {
            var initResponse = await vaultClient.V1.System.InitAsync(new InitOptions
            {
                SecretShares = SECRET_SHARES,
                SecretThreshold = SECRET_THRESHOLD,
            });

            WriteKeys(initResponse.MasterKeys);

            //  Unseal Vault so we can create the policy.
            for (int i = 0; i < SECRET_THRESHOLD; ++i)
            {
                await Unseal(initResponse.MasterKeys[i]);
            }

            Login(initResponse.RootToken);

            await CreateVaultServicePolicyAsync();

            var vaultServiceToken = await CreateVaultServiceToken(initResponse.RootToken);

            await CreateTemplatedWalletPolicyAsync();
            await RevokeToken(initResponse.RootToken);
            await Seal();
        }

        private void Login(string token)
        {
            var vaultClientSettings = new VaultClientSettings(VAULT_ENDPOINT, new TokenAuthMethodInfo(token));
            vaultClient = new VaultClient(vaultClientSettings);
        }

        private static void WriteKeys(ICollection<string> keys)
        {
            PhysicalConsole.Singleton.ResetColor();
            PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.DarkRed;
            PhysicalConsole.Singleton.WriteLine("###########################################################");
            PhysicalConsole.Singleton.WriteLine("#                   !!! ATTENTION !!!                     #");
            PhysicalConsole.Singleton.WriteLine("###########################################################");
            PhysicalConsole.Singleton.WriteLine("    We noticed this is the FIRST time you've started       ");
            PhysicalConsole.Singleton.WriteLine("    the Tangram wallet. Your wallet is encrypted in        ");
            PhysicalConsole.Singleton.WriteLine("    Vault using Shamir's secret sharing algorithm.         ");
            PhysicalConsole.Singleton.WriteLine("    Please store all of the following keys in a safe       ");
            PhysicalConsole.Singleton.WriteLine("    place. When unsealing the vault you may use any        ");
            PhysicalConsole.Singleton.WriteLine("    1 of these keys. THESE ARE NOT RECOVERY KEYS.          ");
            PhysicalConsole.Singleton.WriteLine();
            PhysicalConsole.Singleton.WriteLine();

            int i = 1;
            foreach (var key in keys)
            {
                PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.Red;
                PhysicalConsole.Singleton.WriteLine($"KEY {i}: {key}");
                ++i;
            }

            PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.DarkRed;
            PhysicalConsole.Singleton.WriteLine();
            PhysicalConsole.Singleton.WriteLine();
            PhysicalConsole.Singleton.WriteLine("    You will need to unseal the Vault everytime you        ");
            PhysicalConsole.Singleton.WriteLine("    launch the CLI Wallet.                                 ");
            PhysicalConsole.Singleton.WriteLine("    Please type `vault unseal` to unseal the Vault.        ");
            PhysicalConsole.Singleton.WriteLine("###########################################################");
            PhysicalConsole.Singleton.WriteLine("#                   !!! ATTENTION !!!                     #");
            PhysicalConsole.Singleton.WriteLine("###########################################################");
        }

        private async Task<Auth> CreateVaultServiceToken(string authToken)
        {
            return await CreateToken(authToken, new List<string> { "servicepolicy" });
        }

        private static async Task<Auth> CreateToken(string authToken, List<string> policies, bool orphaned = true)
        {
            var baseUri = new Uri(VAULT_ENDPOINT);
            var uri = new Uri(baseUri, "/v1/auth/token/create");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Vault-Token", authToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                dynamic token = new
                {
                    policies = policies,
                    renewable = true
                };

                string json = JsonConvert.SerializeObject(token, Formatting.Indented);

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(uri, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var tokenCreateRes = JsonConvert.DeserializeObject<VaultTokenCreateResponse>(content);

                    return tokenCreateRes.auth;
                }
            }

            throw new Exception("Unexpected response when attempting to create token");
        }

        private async Task CreateVaultServicePolicyAsync()
        {
            PhysicalConsole.Singleton.ResetColor();
            PhysicalConsole.Singleton.WriteLine("Creating Vault Service Policy");

            dynamic policy = new JObject();

            policy.path = new JObject();
            policy.path["auth/userpass/users/*"] = new JObject();
            policy.path["auth/userpass/users/*"]["capabilities"] = new JArray(new string[] { "create", "list" });

            policy.path["identity/*"] = new JObject();
            policy.path["identity/*"]["capabilities"] = new JArray(new string[] { "create", "update" });

            policy.path["secret/wallets/*"] = new JObject();
            policy.path["secret/wallets/*"]["capabilities"] = new JArray(new string[] { "list" });

            policy.path["secret/data/wallets/*"] = new JObject();
            policy.path["secret/data/wallets/*"]["capabilities"] = new JArray(new string[] { "list" });

            policy.path["sys/auth"] = new JObject();
            policy.path["sys/auth"]["capabilities"] = new JArray(new string[] { "read" });

            var policyJSON = policy.ToString(Newtonsoft.Json.Formatting.None);

            await vaultClient.V1.System.WritePolicyAsync(new Policy { Name = "servicepolicy", Rules = policyJSON });
        }

        private async Task CreateTemplatedWalletPolicyAsync()
        {
            PhysicalConsole.Singleton.ResetColor();
            PhysicalConsole.Singleton.WriteLine("Creating Templated Wallet Policy");

            dynamic policy = new JObject();

            policy.path = new JObject();
            policy.path["secret/wallets/{{identity.entity.name}}/*"] = new JObject();
            policy.path["secret/wallets/{{identity.entity.name}}/*"]["capabilities"] = new JArray(new string[] { "create", "read", "update", "delete", "list" });

            policy.path["secret/data/wallets/{{identity.entity.name}}/*"] = new JObject();
            policy.path["secret/data/wallets/{{identity.entity.name}}/*"]["capabilities"] = new JArray(new string[] { "create", "read", "update", "delete", "list" }); ;

            var policyJSON = policy.ToString(Newtonsoft.Json.Formatting.None);

            await vaultClient.V1.System.WritePolicyAsync(new Policy { Name = "walletpolicy", Rules = policyJSON });
        }
    }
}
