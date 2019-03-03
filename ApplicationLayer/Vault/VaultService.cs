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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TangramCypher.ApplicationLayer.Vault.Models;
using TangramCypher.Helper;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SystemBackend;

namespace TangramCypher.ApplicationLayer.Vault
{
    public class VaultService : HostedService, IVaultService, IDisposable
    {
        private static readonly DirectoryInfo userDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
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

        public void StartVaultService()
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
            else
            {
                throw new Exception("Unable to find Vault executable.");
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
        }

        private async Task ContinueInitialization()
        {
            logger.LogInformation("Checking Vault Init Status");

            if (!await vaultClient.V1.System.GetInitStatusAsync())
            {
                logger.LogInformation("Vault not Initialized... Initializing");

                await Init();

                //  TODO: Find a better way, this is necessary because the VaultProcess is outputting to the console.
                //  However, without this hack the user won't know they can start typing.
                console.ForegroundColor = ConsoleColor.Cyan;
                console.Write("tangram$ ");
                console.ResetColor();
            }
            else
            {
                if (shardFile.Exists)
                {
                    logger.LogInformation("Shard file exists");

                    shard = await File.ReadAllTextAsync(shardFile.FullName);
                    await Unseal(shard);
                }
                else
                {
                    logger.LogWarning("Unable to find Vault shard file.");
                }

                if (serviceTokenFile.Exists)
                {
                    logger.LogInformation("Service token file exists");

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
            logger.LogInformation($"WorkingDirectory: {tangramDirectory.FullName}");

            vaultProcess = new Process();
            vaultProcess.StartInfo.FileName = vaultExecutable.FullName;
            vaultProcess.StartInfo.Arguments = $"server -config {tangramDirectory.FullName}vault.json";
            vaultProcess.StartInfo.UseShellExecute = false;
            vaultProcess.StartInfo.CreateNoWindow = true;
            vaultProcess.StartInfo.RedirectStandardOutput = true;
            vaultProcess.OutputDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogInformation(e.Data);
                    console.Write(e.Data);
                }

                if (e != null && e.Data != null)
                {
                    if (e.Data.Contains("Vault server started!"))
                    {
                        console.ResetColor();
                        console.WriteLine("Vault Server Started!");
                        logger.LogInformation("Vault Server Started!");

                        await ContinueInitialization();
                    }
                }
            };

            vaultProcess.Start();
            vaultProcess.BeginOutputReadLine();
        }

        public async Task Unseal(string shard, bool skipPrint = false)
        {
            var response = await PutAsJsonAsync<SealStatus>(new VaultUnsealRequest { key = shard, reset = false }, "/v1/sys/unseal");

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
            var response = await PutAsJsonAsync<string>(new VaultLeaseRevokeRequest { lease_id = token }, "/v1/sys/leases/revoke", token);
        }

        public async Task Init()
        {
            logger.LogInformation($"Initializing Vault with {secretThreshold} of {secretShares} secret shares.");

            var initResponse = await vaultClient.V1.System.InitAsync(new InitOptions
            {
                SecretShares = secretShares,
                SecretThreshold = secretThreshold,
            });

            var userKeys = initResponse.MasterKeys.OfType<string>().ToList().Skip(1).ToArray();

            var serviceShard = initResponse.MasterKeys.First();

            logger.LogInformation("Writing Vault Shard to disk");

            File.WriteAllText(shardFile.FullName, serviceShard);

            logger.LogInformation("Printing secret shares to User");

            WriteKeys(userKeys);

            logger.LogInformation("Temporarily unsealing the Vault to continue setup process");

            //  Unseal Vault so we can create the policy.
            for (int i = 0; i < secretThreshold; ++i)
            {
                await Unseal(initResponse.MasterKeys[i], true);
            }

            logger.LogInformation("Logging in using root token");
            Login(initResponse.RootToken);

            await CreateVaultServicePolicyAsync(initResponse.RootToken);

            serviceToken = await CreateVaultServiceToken(initResponse.RootToken);
            var vaultServiceSerialized = JsonConvert.SerializeObject(serviceToken);

            logger.LogInformation("Writing Vault Service Token to disk");
            File.WriteAllText(serviceTokenFile.FullName, vaultServiceSerialized);

            await CreateTemplatedWalletPolicyAsync(initResponse.RootToken);
            await EnableUserpassAuth();

            logger.LogInformation("Revoking root token");
            await RevokeToken(initResponse.RootToken);

            //  Reseal the Vault.

            logger.LogInformation("Sealing the Vault");
            await Seal();

            //  Partially unseal using the stored shard
            await Unseal(serviceShard);
            console.ForegroundColor = ConsoleColor.DarkRed;
            console.WriteLine("Plase type `vault unseal` to unseal the vault.");
            console.ResetColor();
        }

        private async Task EnableUserpassAuth()
        {
            logger.LogInformation("Enabling Userpass Auth");

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
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentNullException(nameof(authToken));
            }

            logger.LogInformation("Creating Vault Service Token");
            return await CreateToken(authToken, new List<string> { "servicepolicy" });
        }

        private async Task<T> PostAsJsonAsync<T>(object obj, string requestUri, string authToken = null)
        {
            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, requestUri);

            using (var client = new HttpClient())
            {
                logger.LogInformation($"PostAsJsonAsync {requestUri}");

                if (!string.IsNullOrEmpty(authToken))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", authToken);

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string json = JsonConvert.SerializeObject(obj, Formatting.None);

                logger.LogInformation($"POST {json} to {requestUri}");

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(uri, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<T>(content);
                }

                throw new Exception($"Error: server returned status code {response.StatusCode}");
            }
        }

        private async Task<T> PutAsJsonAsync<T>(object obj, string requestUri, string authToken = null)
        {
            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, requestUri);

            using (var client = new HttpClient())
            {
                logger.LogInformation($"PutAsJsonAsync {requestUri}");

                if (!string.IsNullOrEmpty(authToken))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", authToken);

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string json = JsonConvert.SerializeObject(obj, Formatting.None);

                logger.LogInformation($"PUT {json} to {requestUri}");

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(uri, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<T>(content);
                }

                throw new Exception($"Error: server returned status code {response.StatusCode}");
            }
        }


        private async Task<T> GetAsJsonAsync<T>(string requestUri, string authToken = null)
        {
            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, requestUri);

            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(authToken))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", authToken);

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(uri);

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

            var request = new VaultTokenCreateRequest
            {
                policies = policies,
                renewable = true
            };

            console.WriteLine("Created dynamic token");

            var response = await PostAsJsonAsync<VaultTokenCreateResponse>(request, "/v1/auth/token/create", authToken);

            return response.auth;
        }

        private async Task CreateVaultServicePolicyAsync(string rootToken)
        {
            logger.LogInformation("Creating Vault Service Policy");
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

            logger.LogInformation("Creating Policy object");

            var data = new VaultPolicyCreateRequest { policy = policy.ToString() };

            logger.LogInformation("Created Policy object");

            var response = await PutAsJsonAsync<string>(data, "/v1/sys/policy/servicepolicy", rootToken);
        }

        private async Task CreateTemplatedWalletPolicyAsync(string rootToken)
        {
            console.ResetColor();
            logger.LogInformation("Creating Templated Wallet Policy");
            console.WriteLine("Creating Templated Wallet Policy");

            dynamic policy = new JObject();

            policy.path = new JObject();
            policy.path["secret/wallets/{{identity.entity.name}}/*"] = new JObject();
            policy.path["secret/wallets/{{identity.entity.name}}/*"]["capabilities"] = new JArray(new string[] { "create", "read", "update", "delete", "list" });

            policy.path["secret/data/wallets/{{identity.entity.name}}/*"] = new JObject();
            policy.path["secret/data/wallets/{{identity.entity.name}}/*"]["capabilities"] = new JArray(new string[] { "create", "read", "update", "delete", "list" }); ;

            var data = new VaultPolicyCreateRequest { policy = policy.ToString() };

            var response = await PutAsJsonAsync<string>(data, "/v1/sys/policy/walletpolicy", rootToken);
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

            await PostAsJsonAsync<object>(new VaultUserCreateRequest { password = password }, $"v1/auth/userpass/users/{username}", serviceToken.client_token);

            var identityCreateResponse = await PostAsJsonAsync<VaultIdentityEntityCreateResponse>
            (
                new VaultIdentityEntityCreateRequest
                {
                    name = username,
                    policies = new string[] { "walletpolicy" }
                },
                $"v1/identity/entity",
                serviceToken.client_token
            );

            var authResponse = await GetAsJsonAsync<JObject>($"v1/sys/auth", serviceToken.client_token);

            var accesor = authResponse["userpass/"]["accessor"].Value<string>();

            var entityId = identityCreateResponse.data.id;

            var identityAliasCreateResponse = await PostAsJsonAsync<object>
            (
                new VaultCreateEntityAliasRequest
                {
                    name = username,
                    canonical_id = entityId,
                    mount_accessor = accesor
                },
                $"v1/identity/entity-alias",
                serviceToken.client_token
            );
        }

        public async Task SaveDataAsync(string username, string password, string path, IDictionary<string, object> data)
        {
            var vaultClientSettings = new VaultClientSettings(endpoint, new UserPassAuthMethodInfo(username,
                                                                                                   password));
            var vaultWalletClient = new VaultClient(vaultClientSettings);

            await vaultWalletClient.V1.Secrets.KeyValue.V1.WriteSecretAsync(path, data);
        }

        public async Task<Secret<Dictionary<string, object>>> GetDataAsync(string username, string password, string path)
        {
            var vaultClientSettings = new VaultClientSettings(endpoint, new UserPassAuthMethodInfo(username,
                                                                                                   password));
            var vaultWalletClient = new VaultClient(vaultClientSettings);

            var secret = await vaultWalletClient.V1.Secrets.KeyValue.V1.ReadSecretAsync(path);

            return secret;
        }

        public async Task<Secret<ListInfo>> GetListAsync(string path)
        {
            return await vaultClient.V1.Secrets.KeyValue.V1.ReadSecretPathsAsync(path);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => StartVaultService());
            }
            catch(Exception e)
            {
                Util.LogException(console, logger, e);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (vaultProcess != null)
            {
                vaultProcess.Kill();
                vaultProcess.Dispose();
                vaultProcess = null;
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
