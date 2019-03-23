// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sodium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
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

        private string VaultVersion = "1.0.3";

        private string VaultExecutableHash
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    return "1F3AA640273A90FBA56AE60D06AD15E9D42CA073148CE3F39C800D81D0949682";
                }

                throw new Exception("Unable to determine vault executable based on platform");
            }
        }

        private string VaultExecutableUrl
        {
            get
            {
                var filename = DownloadFilename;

                return $"https://releases.hashicorp.com/vault/{VaultVersion}/{filename}";
            }
        }

        private string DownloadFilename
        {
            get
            {
                if (string.IsNullOrEmpty(VaultVersion))
                {
                    throw new ArgumentNullException(nameof(VaultVersion));
                }

                string os = Util.GetOSPlatform().ToString().ToLowerInvariant();
                string architecture = null;

                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.X64:
                        architecture = "amd64";
                        break;
                    case Architecture.X86:
                        architecture = "386";
                        break;
                    case Architecture.Arm:
                        architecture = "arm";
                        break;
                }

                if (string.IsNullOrEmpty(architecture))
                {
                    throw new Exception("Unable derive supported architecture");
                }

                return $"vault_{VaultVersion}_{os}_{architecture}.zip";
            }
        }

        private Process vaultProcess;

        private IConsole console;
        private ILogger logger;

        private readonly int secretShares;
        private readonly int secretThreshold;

        private readonly string endpoint;
        private readonly int startTimeout;


        //  TODO: Wrap serviceToken in secure string;
        private VaultTokenCreateResponseAuth serviceToken;
        private VaultClientSettings vaultClientSettings;

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

            vaultClientSettings = new VaultClientSettings(endpoint, null);
        }

        private static string VaultExecutableName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "vault.exe";
                }

                return "vault";
            }
        }

        private void DownloadVault()
        {
            using (var webClient = new WebClient())
            {
                console.WriteLine($"Downloading {VaultExecutableUrl}");

                var data = webClient.DownloadData(VaultExecutableUrl);

                File.WriteAllBytes(DownloadFilename, data);

                console.WriteLine($"Extracting {DownloadFilename}...");

                ZipFile.ExtractToDirectory(DownloadFilename, ".", true);

                console.WriteLine($"Finished Extracting {DownloadFilename}");

                File.Delete(DownloadFilename);

                console.WriteLine($"Deleted {DownloadFilename}");
            }
        }

        public void StartVaultService()
        {
            //  Find Vault Executable
            if (VaultExecutable != null)
            {
                var checksum = Util.GetFileHash(VaultExecutable).ToUpperInvariant();

                if (checksum != VaultExecutableHash)
                {
                    console.WriteLine("Your vault doesn't match the expected version. Automatically downloading...");

                    DownloadVault();
                }
                else
                {
                    console.WriteLine("Vault version matches expected hash.");
                }
            }
            else
            {
                console.WriteLine("Unable to find Vault executable. Attempting to automatically download...");
                DownloadVault();
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

                RestartVaultProcess(vaultProcess);
            }
            else
            {
                StartVaultProcess();
            }
        }

        private FileInfo VaultExecutable
        {
            get
            {
                var fileInfo = tangramDirectory.GetFiles(VaultExecutableName, SearchOption.TopDirectoryOnly);

                if (fileInfo != null && fileInfo.Length == 1)
                {
                    return fileInfo[0];
                }

                return null;
            }
        }

        private async Task ContinueInitialization()
        {
            logger.LogInformation("Checking Vault Init Status");

            var vc = new VaultClient(vaultClientSettings);

            if (!await vc.V1.System.GetInitStatusAsync())
            {
                logger.LogInformation("Vault not Initialized... Initializing");

                await Init();
            }
            else
            {
                if (shardFile.Exists)
                {
                    logger.LogInformation("Shard file exists");

                    using (var shard = new SecureString())
                    {
                        foreach (char c in File.OpenText(shardFile.FullName).ReadToEnd().ToCharArray())
                        {
                            shard.AppendChar(c);
                        }
                        await Unseal(shard);
                    }
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
                }
                else
                {
                    throw new Exception("Error: Vault is initialized but required service token is missing.");
                }
            }

            using (var ct = serviceToken.client_token.ToSecureString())
            {
                await AskForVaultUnseal(ct);
            }
        }

        private void RestartVaultProcess(Process vaultProcess)
        {
            if(vaultProcess == null)
            {
                throw new ArgumentNullException(nameof(vaultProcess));
            }

            console.ForegroundColor = ConsoleColor.Yellow;
            console.WriteLine($"Restarting Vault Process.");
            console.ResetColor();
            logger.LogWarning($"Restarting Vault Process.");

            vaultProcess.Kill();

            StartVaultProcess();
        }

        private void StartVaultProcess()
        {
            logger.LogInformation($"WorkingDirectory: {tangramDirectory.FullName}");

            vaultProcess = new Process();
            vaultProcess.StartInfo.FileName = VaultExecutable.FullName;
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

        public async Task Unseal(SecureString shard, bool skipPrint = false)
        {
            using (var s = shard.Insecure())
            {
                if (s == null && string.IsNullOrEmpty(s.Value))
                {
                    throw new ArgumentNullException(nameof(shard));
                }

                var response = await PutAsJsonAsync<SealStatus>(new VaultUnsealRequest { key = s.Value, reset = false }, "/v1/sys/unseal");

                if (!response.Sealed && !skipPrint)
                {
                    console.ResetColor();
                    console.ForegroundColor = ConsoleColor.DarkGreen;
                    console.WriteLine("Vault Unsealed!");
                }
            }
        }

        public async Task Seal(SecureString token)
        {
            using (var t = token.Insecure())
            {
                var vaultClientSettings = new VaultClientSettings(endpoint, new TokenAuthMethodInfo(t.Value));

                var vc = new VaultClient(vaultClientSettings);
                await vc.V1.System.SealAsync();
            }
        }

        public async Task RevokeToken(SecureString token)
        {
            console.WriteLine("Revoking Root Token");

            using (var t = token.Insecure())
            {
                var response = await PutAsJsonAsync<string>(new VaultLeaseRevokeRequest { lease_id = t.Value }, "/v1/sys/leases/revoke", token);
            }
        }

        public async Task Init()
        {
            logger.LogInformation($"Initializing Vault with {secretThreshold} of {secretShares} secret shares.");

            var vc = new VaultClient(vaultClientSettings);

            var initResponse = await vc.V1.System.InitAsync(new InitOptions
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
                using (var mk = initResponse.MasterKeys[i].ToSecureString())
                {
                    await Unseal(mk, true);
                }
            }

            logger.LogInformation("Logging in using root token");
            using (var rt = initResponse.RootToken.ToSecureString())
            {
                await CreateVaultServicePolicyAsync(rt);

                serviceToken = await CreateVaultServiceToken(rt);
                var vaultServiceSerialized = JsonConvert.SerializeObject(serviceToken);

                logger.LogInformation("Writing Vault Service Token to disk");
                File.WriteAllText(serviceTokenFile.FullName, vaultServiceSerialized);

                await CreateTemplatedWalletPolicyAsync(rt);
                await EnableUserpassAuth(rt);

                logger.LogInformation("Revoking root token");
                await RevokeToken(rt);

                //  Reseal the Vault.

                logger.LogInformation("Sealing the Vault");
                await Seal(rt);
            }

            using (var ss = serviceShard.ToSecureString())
            {
                //  Partially unseal using the stored shard
                await Unseal(ss);
            }
        }

        private async Task AskForVaultUnseal(SecureString token)
        {
            using (var t = token.Insecure())
            {
                var vaultClientSettings = new VaultClientSettings(endpoint, new TokenAuthMethodInfo(t.Value));

                var vc = new VaultClient(vaultClientSettings);

                var healthStatus = await vc.V1.System.GetHealthStatusAsync();

                if (healthStatus.Sealed && healthStatus.Initialized)
                {
                    console.ForegroundColor = ConsoleColor.Yellow;

                    console.WriteLine("Vault is currently sealed.");
                    console.WriteLine("Please type `vault unseal` to begin unsealing the vault.");
                    console.ResetColor();
                }

                //  TODO: Find a better way, this is necessary because the VaultProcess is outputting to the console.
                //  However, without this hack the user won't know they can start typing.
                console.ForegroundColor = ConsoleColor.Cyan;
                console.Write($"{Environment.NewLine}tangram$ ");
                console.ResetColor();
            }
        }

        private async Task EnableUserpassAuth(SecureString rt)
        {
            logger.LogInformation("Enabling Userpass Auth");

            using (var t = rt.Insecure())
            {
                var vaultClientSettings = new VaultClientSettings(endpoint, new TokenAuthMethodInfo(t.Value));

                var vc = new VaultClient(vaultClientSettings);
                await vc.V1.System.MountAuthBackendAsync(new VaultSharp.V1.AuthMethods.AuthMethod()
                {
                    Path = "userpass",
                    Type = VaultSharp.V1.AuthMethods.AuthMethodType.UserPass,
                    Description = "Userpass Auth"
                });

                var accessor = await vc.V1.System.GetAuthBackendConfigAsync("userpass");
            }
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

        private async Task<VaultTokenCreateResponseAuth> CreateVaultServiceToken(SecureString authToken)
        {
            logger.LogInformation("Creating Vault Service Token");
            return await CreateToken(authToken, new List<string> { "servicepolicy" });
        }

        private async Task<T> PostAsJsonAsync<T>(object obj, string requestUri, SecureString authToken = null)
        {
            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, requestUri);

            using (var client = new HttpClient())
            using (var rt = authToken != null ? authToken.Insecure() : null)
            {
                logger.LogInformation($"PostAsJsonAsync {requestUri}");

                if (authToken != null && !string.IsNullOrEmpty(rt.Value))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", rt.Value);

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

        private async Task<T> PutAsJsonAsync<T>(object obj, string requestUri, SecureString authToken = null)
        {
            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, requestUri);

            using (var client = new HttpClient())
            using (var rt = authToken != null ? authToken.Insecure() : null)
            {
                logger.LogInformation($"PutAsJsonAsync {requestUri}");

                if (authToken != null && !string.IsNullOrEmpty(rt.Value))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", rt.Value);

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


        private async Task<T> GetAsJsonAsync<T>(string requestUri, SecureString authToken = null)
        {
            var baseUri = new Uri(endpoint);
            var uri = new Uri(baseUri, requestUri);

            using (var client = new HttpClient())
            using (var rt = authToken != null ? authToken.Insecure() : null)
            {
                if (authToken != null && !string.IsNullOrEmpty(rt.Value))
                    client.DefaultRequestHeaders.Add("X-Vault-Token", rt.Value);

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

        private async Task<VaultTokenCreateResponseAuth> CreateToken(SecureString authToken, List<string> policies, bool orphaned = true)
        {
            if (authToken == null)
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

        private async Task CreateVaultServicePolicyAsync(SecureString rootToken)
        {
            if (rootToken == null)
            {
                throw new ArgumentNullException(nameof(rootToken));
            }

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

        private async Task CreateTemplatedWalletPolicyAsync(SecureString rootToken)
        {
            if (rootToken == null)
            {
                throw new ArgumentNullException(nameof(rootToken));
            }

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

        public async Task CreateUserAsync(SecureString username, SecureString password)
        {
            using (var u = username.Insecure())
            using (var p = password.Insecure())
            {
                if (string.IsNullOrEmpty(u.Value))
                {
                    throw new ArgumentNullException(nameof(username));
                }

                if (string.IsNullOrEmpty(p.Value))
                {
                    throw new ArgumentNullException(nameof(password));
                }

                using (var ct = serviceToken.client_token.ToSecureString())
                {
                    await PostAsJsonAsync<object>(new VaultUserCreateRequest { password = p.Value }, $"v1/auth/userpass/users/{u.Value}", ct);

                    var identityCreateResponse = await PostAsJsonAsync<VaultIdentityEntityCreateResponse>
                    (
                        new VaultIdentityEntityCreateRequest
                        {
                            name = u.Value,
                            policies = new string[] { "walletpolicy" }
                        },
                        $"v1/identity/entity",
                        ct
                    );

                    var authResponse = await GetAsJsonAsync<JObject>($"v1/sys/auth", ct);

                    var accesor = authResponse["userpass/"]["accessor"].Value<string>();

                    var entityId = identityCreateResponse.data.id;

                    var identityAliasCreateResponse = await PostAsJsonAsync<object>
                    (
                        new VaultCreateEntityAliasRequest
                        {
                            name = u.Value,
                            canonical_id = entityId,
                            mount_accessor = accesor
                        },
                        $"v1/identity/entity-alias",
                        ct
                    );
                }
            }
        }

        public async Task SaveDataAsync(SecureString username, SecureString password, string path, IDictionary<string, object> data)
        {
            using (var u = username.Insecure())
            using (var p = password.Insecure())
            {
                if (string.IsNullOrEmpty(u.Value))
                {
                    throw new ArgumentNullException(nameof(username));
                }

                if (string.IsNullOrEmpty(p.Value))
                {
                    throw new ArgumentNullException(nameof(password));
                }
                var vaultClientSettings = new VaultClientSettings(endpoint, new UserPassAuthMethodInfo(u.Value,
                                                                                                       p.Value));
                var vaultWalletClient = new VaultClient(vaultClientSettings);

                await vaultWalletClient.V1.Secrets.KeyValue.V1.WriteSecretAsync(path, data);
            }
        }

        public async Task<Secret<Dictionary<string, object>>> GetDataAsync(SecureString username, SecureString password, string path)
        {
            using (var u = username.Insecure())
            using (var p = password.Insecure())
            {
                if (string.IsNullOrEmpty(u.Value))
                {
                    throw new ArgumentNullException(nameof(username));
                }

                if (string.IsNullOrEmpty(p.Value))
                {
                    throw new ArgumentNullException(nameof(password));
                }

                var vaultClientSettings = new VaultClientSettings(endpoint, new UserPassAuthMethodInfo(u.Value,
                                                                                                   p.Value));
                var vc = new VaultClient(vaultClientSettings);

                var secret = await vc.V1.Secrets.KeyValue.V1.ReadSecretAsync(path);

                return secret;
            }
        }

        public async Task<Secret<ListInfo>> GetListAsync(string path)
        {
            var t = serviceToken.client_token;
        
            var vaultClientSettings = new VaultClientSettings(endpoint, new TokenAuthMethodInfo(t));

            var vc = new VaultClient(vaultClientSettings);

            return await vc.V1.Secrets.KeyValue.V1.ReadSecretPathsAsync(path);
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
