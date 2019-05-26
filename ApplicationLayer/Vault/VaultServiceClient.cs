using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
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
    public class VaultServiceClient : IVaultServiceClient
    {
        private IConsole console;
        private ILogger logger;

        private readonly string endpoint;
        private VaultClientSettings vaultClientSettings;

        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        private static readonly FileInfo serviceTokenFile = new FileInfo(Path.Combine(tangramDirectory.FullName, "servicetoken"));

        public async Task<VaultTokenCreateResponseAuth> GetServiceTokenAsync()
        {
            if (serviceTokenFile.Exists)
            {
                logger.LogInformation("Service token file exists");

                var serviceTokenJson = await File.ReadAllTextAsync(serviceTokenFile.FullName);
                return JsonConvert.DeserializeObject<VaultTokenCreateResponseAuth>(serviceTokenJson);
            }
            else
            {
                throw new Exception("Error: unable to find Vault service token file. Has Vault finished initializing?");
            }
        }

        public VaultServiceClient(IConfiguration configuration, IConsole cnsl, ILogger lgr)
        {
            console = cnsl;
            logger = lgr;

            var vault_section = configuration.GetSection("vault");
            endpoint = vault_section.GetValue<string>("endpoint");

            var children = configuration.GetChildren();

            vaultClientSettings = new VaultClientSettings(endpoint, null);
        }

        public async Task<bool> Unseal(SecureString shard, bool skipPrint = false)
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

                return !response.Sealed;
            }
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

                var serviceToken = await GetServiceTokenAsync();

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
            var serviceToken = await GetServiceTokenAsync();

            var t = serviceToken.client_token;

            var vaultClientSettings = new VaultClientSettings(endpoint, new TokenAuthMethodInfo(t));

            var vc = new VaultClient(vaultClientSettings);

            return await vc.V1.Secrets.KeyValue.V1.ReadSecretPathsAsync(path);
        }

        public async Task<T> PostAsJsonAsync<T>(object obj, string requestUri, SecureString authToken = null)
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

        public async Task<T> PutAsJsonAsync<T>(object obj, string requestUri, SecureString authToken = null)
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

        public async Task<T> GetAsJsonAsync<T>(string requestUri, SecureString authToken = null)
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
    }
}
