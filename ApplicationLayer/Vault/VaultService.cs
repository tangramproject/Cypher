using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault.Models;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SystemBackend;

namespace TangramCypher.ApplicationLayer.Vault
{
    public class VaultService : IVaultService
    {
        private static readonly DirectoryInfo userDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(Path.Combine(userDirectory.FullName, ".tangramcli"));
        private static readonly FileInfo shardFile = new FileInfo(Path.Combine(tangramDirectory.FullName, "shard"));
        private static readonly FileInfo serviceTokenFile = new FileInfo(Path.Combine(tangramDirectory.FullName, "servicetoken"));

        private FileInfo vaultExecutable;
        private Process vaultProcess;

        private IVaultClient vaultClient;
        private IConsole console;
        private ILogger logger;

        private readonly int secretShares;
        private readonly int secretThreshold;

        private readonly string endpoint;
        private readonly int startTimeout;

        private string shard;
        private VaultTokenCreateResponseAuth serviceToken;

        public VaultService(IConfiguration configuration, IConsole cnsl, ILogger lgr)
        {
            console = cnsl;
            logger = lgr;

            var vault_section = configuration.GetSection("vault");

            endpoint = vault_section.GetValue<string>("endpoint");
            startTimeout = vault_section.GetValue<int>("start_timeout");
            secretShares = vault_section.GetValue<int>("num_secret_shares");
            secretThreshold = vault_section.GetValue<int>("num_secret_threshold");

            var children = configuration.GetChildren();

            var vaultClientSettings = new VaultClientSettings(endpoint, null);
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
            console.ResetColor();
            console.WriteLine("Starting Vault Service.");

            var vaultProcesses = Process.GetProcessesByName("vault");

            if (vaultProcesses.Length == 1)
            {
                vaultProcess = vaultProcesses[0];

                console.ForegroundColor = ConsoleColor.Yellow;
                console.WriteLine($"Existing Vault Process Detected.{Environment.NewLine}" +
                    $"Please be sure to type `exit` to close the wallet properly.");
                console.ResetColor();
                logger.LogWarning($"Existing Vault Process Detected.{Environment.NewLine}" +
                    $"Please be sure to type `exit` to close the wallet properly.");
            }
            else
            {
                StartVaultProcess();
            }

            if (!await vaultClient.V1.System.GetInitStatusAsync())
            {
                await Init();
            }
            else
            {
                if (shardFile.Exists)
                {
                    shard = await File.ReadAllTextAsync(shardFile.FullName);
                    await Unseal(shard);
                }
                else
                {
                   logger.LogWarning("Shard file missing from disk.");
                }

                if (serviceTokenFile.Exists)
                {
                    var serviceTokenJson = await File.ReadAllTextAsync(serviceTokenFile.FullName);
                    serviceToken = JsonConvert.DeserializeObject<VaultTokenCreateResponseAuth>(serviceTokenJson);

                    Login(serviceToken.client_token);
                }
                else
                {
                    throw new Exception("Error: Vault is initialized but required service token is missing.");
                }
            }
        }

        private void StartVaultProcess()
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

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!vaultProcess.StandardOutput.EndOfStream)
            {
                if (sw.ElapsedMilliseconds > startTimeout) throw new TimeoutException("Timed out waiting for Vault Server to start.");

                string line = vaultProcess.StandardOutput.ReadLine();

                if (line.Contains("Vault server started!"))
                {
                    console.ResetColor();
                    console.WriteLine("Vault Server Started!");
                    logger.LogInformation("Vault Server Started!");
                    break;
                }
            }
        }

        public async Task Unseal(string shard, bool skipPrint = false)
        {
            var unsealTask = await vaultClient.V1.System.UnsealAsync(shard);

            var response = unsealTask;

            if (!response.Sealed && !skipPrint)
            {
                console.ResetColor();
                console.ForegroundColor = ConsoleColor.DarkGreen;
                console.WriteLine("Vault Unsealed!");
            }
        }

        public async Task Seal()
        {
            await vaultClient.V1.System.SealAsync();
        }

        public async Task RevokeToken(string token)
        {
            console.WriteLine("Revoking Root Token");
            await vaultClient.V1.System.RevokeLeaseAsync(token);
        }

        public async Task Init()
        {
            var initResponse = await vaultClient.V1.System.InitAsync(new InitOptions
            {
                SecretShares = secretShares,
                SecretThreshold = secretThreshold,
            });

            var userKeys = initResponse.MasterKeys.OfType<string>().ToList().Skip(1).ToArray();

            var serviceShard = initResponse.MasterKeys.First();

            File.WriteAllText(shardFile.FullName, serviceShard);

            WriteKeys(userKeys);

            //  Unseal Vault so we can create the policy.
            for (int i = 0; i < secretThreshold; ++i)
            {
                await Unseal(initResponse.MasterKeys[i], true);
            }

            Login(initResponse.RootToken);

            await CreateVaultServicePolicyAsync();

            serviceToken = await CreateVaultServiceToken(initResponse.RootToken);
            var vaultServiceSerialized = JsonConvert.SerializeObject(serviceToken);

            File.WriteAllText(serviceTokenFile.FullName, vaultServiceSerialized);

            await CreateTemplatedWalletPolicyAsync();
            await EnableUserpassAuth();
            await RevokeToken(initResponse.RootToken);

            //  Reseal the Vault.
            await Seal();

            //  Partially unseal using the stored shard
            await Unseal(serviceShard);
        }

        private async Task EnableUserpassAuth()
        {
            await vaultClient.V1.System.MountAuthBackendAsync(new VaultSharp.V1.AuthMethods.AuthMethod()
            {
                Path = "userpass",
                Type = VaultSharp.V1.AuthMethods.AuthMethodType.UserPass,
                Description = "Userpass Auth"
            });

            var accessor = await vaultClient.V1.System.GetAuthBackendConfigAsync("userpass");
        }

        private void Login(string token)
        {
            var vaultClientSettings = new VaultClientSettings(endpoint, new TokenAuthMethodInfo(token));
            vaultClient = new VaultClient(vaultClientSettings);
        }

        private void WriteKeys(ICollection<string> keys)
        {
            console.ResetColor();
            console.ForegroundColor = ConsoleColor.DarkRed;
            console.WriteLine("###########################################################");
            console.WriteLine("#                   !!! ATTENTION !!!                     #");
            console.WriteLine("###########################################################");
            console.WriteLine("    We noticed this is the FIRST time you've started       ");
            console.WriteLine("    the Tangram wallet. Your wallet is encrypted in        ");
            console.WriteLine("    Vault using Shamir's secret sharing algorithm.         ");
            console.WriteLine("    Please store all of the following keys in a safe       ");
            console.WriteLine("    place. When unsealing the vault you may use any        ");
            console.WriteLine("    1 of these keys. THESE ARE NOT RECOVERY KEYS.          ");
            console.WriteLine();
            console.WriteLine();

            int i = 1;
            foreach (var key in keys)
            {
                console.ForegroundColor = ConsoleColor.Red;
                console.WriteLine($"KEY {i}: {key}");
                ++i;
            }

            console.ForegroundColor = ConsoleColor.DarkRed;
            console.WriteLine();
            console.WriteLine();
            console.WriteLine("    You will need to unseal the Vault everytime you        ");
            console.WriteLine("    launch the CLI Wallet.                                 ");
            console.WriteLine("    Please type `vault unseal` to unseal the Vault.        ");
            console.WriteLine("###########################################################");
            console.WriteLine("#                   !!! ATTENTION !!!                     #");
            console.WriteLine("###########################################################");
        }

        private async Task<VaultTokenCreateResponseAuth> CreateVaultServiceToken(string authToken)
        {
            return await CreateToken(authToken, new List<string> { "servicepolicy" });
        }

        private static async Task<T> PostAsJsonAsync<T>(object obj, string requestUri, string authToken = null)
        {
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(authToken))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", authToken);

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string json = JsonConvert.SerializeObject(obj, Formatting.None);

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(requestUri, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<T>(content);
                }

                throw new Exception($"Error: server returned status code {response.StatusCode}");
            }
        }

        private static async Task<T> GetAsJsonAsync<T>(string requestUri, string authToken = null)
        {
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(authToken))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", authToken);

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<T>(content);
                }

                throw new Exception($"Error: server returned status code {response.StatusCode}");
            }
        }

        private async Task<VaultTokenCreateResponseAuth> CreateToken(string authToken, List<string> policies, bool orphaned = true)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentNullException(nameof(authToken));
            }

            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, "/v1/auth/token/create");

            dynamic token = new
            {
                policies,
                renewable = true
            };

            var response = await PostAsJsonAsync<VaultTokenCreateResponse>(token, uri.ToString(), authToken);

            return response.auth;
        }

        private async Task CreateVaultServicePolicyAsync()
        {
            console.ResetColor();
            console.WriteLine("Creating Vault Service Policy");

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

            var policyJSON = policy.ToString(Formatting.None);

            await vaultClient.V1.System.WritePolicyAsync(new Policy { Name = "servicepolicy", Rules = policyJSON });
        }

        private async Task CreateTemplatedWalletPolicyAsync()
        {
            console.ResetColor();
            console.WriteLine("Creating Templated Wallet Policy");

            dynamic policy = new JObject();

            policy.path = new JObject();
            policy.path["secret/wallets/{{identity.entity.name}}/*"] = new JObject();
            policy.path["secret/wallets/{{identity.entity.name}}/*"]["capabilities"] = new JArray(new string[] { "create", "read", "update", "delete", "list" });

            policy.path["secret/data/wallets/{{identity.entity.name}}/*"] = new JObject();
            policy.path["secret/data/wallets/{{identity.entity.name}}/*"]["capabilities"] = new JArray(new string[] { "create", "read", "update", "delete", "list" }); ;

            var policyJSON = policy.ToString(Formatting.None);

            await vaultClient.V1.System.WritePolicyAsync(new Policy { Name = "walletpolicy", Rules = policyJSON });
        }

        public async Task CreateUserAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            var baseUri = new Uri(endpoint);
            var userUri = new Uri(baseUri, $"v1/auth/userpass/users/{username}").ToString();

            await PostAsJsonAsync<object>(new { password }, userUri, serviceToken.client_token);

            var identityEntityUri = new Uri(baseUri, $"v1/identity/entity").ToString();
            var identityCreateResponse = await PostAsJsonAsync<VaultIdentityEntityCreateResponse>(
                new { name = username,
                      policies = new string[] { "walletpolicy" }
                }, 
                identityEntityUri, 
                serviceToken.client_token);

            var authUri = new Uri(baseUri, $"v1/sys/auth").ToString();
            var authResponse = await GetAsJsonAsync<JObject>(authUri, serviceToken.client_token);

            var accesor = authResponse["userpass/"]["accessor"].Value<string>();

            var entityId = identityCreateResponse.data.id;

            var identityAliasUri = new Uri(baseUri, $"v1/identity/entity-alias").ToString();
            var identityAliasCreateResponse = await PostAsJsonAsync<object>(new { name = username,
                                                                                  canonical_id = entityId,
                                                                                  mount_accessor = accesor }, 
                                                                            identityAliasUri, 
                                                                            serviceToken.client_token);
        }

        public async Task SaveDataAsync(string username, string password, string path, IDictionary<string, object> data)
        {
            var vaultClientSettings = new VaultClientSettings(endpoint, new UserPassAuthMethodInfo(username, 
                                                                                                   password));
            var vaultWalletClient = new VaultClient(vaultClientSettings);

            await vaultWalletClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, data);
        }

        public async Task<Secret<SecretData>> GetDataAsync(string username, string password, string path)
        {
            var vaultClientSettings = new VaultClientSettings(endpoint, new UserPassAuthMethodInfo(username,
                                                                                                   password));
            var vaultWalletClient = new VaultClient(vaultClientSettings);

            var secret = await vaultWalletClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path);

            return secret;
        }

        public async Task<Secret<ListInfo>> GetListAsync(string path)
        {
            return await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(path);
        }
    }
}
