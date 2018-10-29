using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DotNetTor.SocksPort;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace Cypher.ApplicationLayer.Onion
{
    public class OnionService : IOnionService, IDisposable
    {
        const string ONION = "onion";
        const string SOCKS_HOST = "onion_socks_host";
        const string SOCKS_PORT = "onion_socks_port";
        const string CONTROL_HOST = "onion_control_host";
        const string CONTROL_PORT = "onion_control_port";
        const string HASHED_CONTROL_PASSWORD = "onion_hashed_control_password";
        const string ONION_TORRC = "onion/torrc";

        readonly ICryptography cryptography;
        readonly IConfigurationSection onionSection;
        readonly ILogger logger;
        readonly string socksHost;
        readonly int socksPort;
        readonly string controlHost;
        readonly int controlPort;
        readonly string hashedPassword;
        readonly string onionDirectory;
        readonly string torrcPath;

        Process TorProcess { get; set; }

        public OnionService(ICryptography cryptography, IConfiguration configuration, ILogger logger)
        {
            this.cryptography = cryptography;
            onionSection = configuration.GetSection(ONION);

            this.logger = logger;

            socksHost = onionSection.GetValue<string>(SOCKS_HOST);
            socksPort = onionSection.GetValue<int>(SOCKS_PORT);
            controlHost = onionSection.GetValue<string>(CONTROL_HOST);
            controlPort = onionSection.GetValue<int>(CONTROL_PORT);
            hashedPassword = onionSection.GetValue<string>(HASHED_CONTROL_PASSWORD);

            onionDirectory = Path.Combine(Util.EntryAssemblyPath(), ONION);
            torrcPath = Path.Combine(Util.EntryAssemblyPath(), ONION_TORRC);
        }

        public async Task<string> GetAsync(string url, object data)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("message", nameof(url));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                using (var httpClient = new HttpClient(new SocksPortHandler(socksHost, socksPort)))
                {
                    string requestUrl = $"{url}?{GetQueryString(data)}";

                    logger.LogInformation($"GetAsync Start, requestUrl:{requestUrl}");

                    var response = await httpClient.GetAsync(requestUrl).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();

                    logger.LogInformation($"GetAsync End, requestUrl:{requestUrl}, HttpStatusCode:{response.StatusCode}, result:{result}");

                    return result;
                }

            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    string responseContent = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    throw new Exception($"Response :{responseContent}", ex);
                }
                throw;
            }
        }

        public void ChangeCircuit(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("message", nameof(password));
            }

            try
            {
                var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, password);
                controlPortClient.ChangeCircuitAsync().Wait();
            }
            catch (DotNetTor.TorException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public string GenerateHashPassword(string password)
        {
            var torProcessStartInfo = new ProcessStartInfo("tor")
            {
                Arguments = $"--hash-password {password}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            TorProcess = Process.Start(torProcessStartInfo);

            var sOut = TorProcess.StandardOutput;
            var result = sOut.ReadToEnd();

            if (!TorProcess.HasExited)
            {
                TorProcess.Kill();
            }

            sOut.Close();
            TorProcess.Close();

            return result;
        }

        void SendCommands(string command, string password)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("message", nameof(command));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("message", nameof(password));
            }

            try
            {
                var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, password);
                var result = controlPortClient.SendCommandAsync(command).GetAwaiter().GetResult();
            }
            catch (DotNetTor.TorException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<T> PostAsync<T>(string url, object data) where T : class, new()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("message", nameof(url));
            }

            try
            {
                using (var httpClient = new HttpClient(new SocksPortHandler(socksHost, socksPort)))
                {
                    string content = JsonConvert.SerializeObject(data);
                    var buffer = Encoding.UTF8.GetBytes(content);
                    var byteContent = new ByteArrayContent(buffer);

                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = await httpClient.PostAsync(url, byteContent).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        logger.LogError($"GetAsync End, url:{url}, HttpStatusCode:{response.StatusCode}, result:{result}");
                        return new T();
                    }

                    logger.LogInformation($"GetAsync End, url:{url}, result:{result}");

                    return JsonConvert.DeserializeObject<T>(result);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    string responseContent = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    throw new Exception($"response :{responseContent}", ex);
                }
                throw;
            }
        }

        public void StartOnion(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("message", nameof(password));
            }

            CreateTorrc();

            TorProcess = null;
            var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, password);

            try
            {
                controlPortClient.IsCircuitEstablishedAsync().GetAwaiter().GetResult();
            }
            catch
            {
                var torProcessStartInfo = new ProcessStartInfo("tor")
                {
                    Arguments = $"-f { onionDirectory }",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                TorProcess = Process.Start(torProcessStartInfo);

                Task.Delay(3000).GetAwaiter().GetResult();

                var established = false;
                var count = 0;

                while (!established)
                {
                    if (count >= 21) throw new Exception("Couldn't establish circuit in time.");

                    established = controlPortClient.IsCircuitEstablishedAsync().GetAwaiter().GetResult();

                    Task.Delay(1000).GetAwaiter().GetResult();
                    count++;
                }
            }
        }

        static string GetQueryString(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var properties = from p in obj.GetType().GetProperties()
                             where p.GetValue(obj, null) != null
                             select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

            return String.Join("&", properties.ToArray());
        }


        void CreateTorrc()
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                throw new ArgumentException("Hashed control password is not set.", nameof(hashedPassword));
            }

            if (!Directory.Exists(onionDirectory))
            {
                try
                {
                    Directory.CreateDirectory(onionDirectory);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    throw new Exception(ex.Message);
                }
            }

            if (File.Exists(torrcPath))
                return;

            var torrcContent = new string[] {
                "AvoidDiskWrites 1",
                string.Format("HashedControlPassword {0}", hashedPassword),
                "SocksPort auto IPv6Traffic PreferIPv6 KeepAliveIsolateSOCKSAuth",
                "ControlPort auto",
                "CookieAuthentication 1",
                "CircuitBuildTimeout 10",
                "KeepalivePeriod 60",
                "NumEntryGuards 8",
                "SocksPort 9050",
                "Log notice stdout"
            };

            try
            {
                using (StreamWriter outputFile = new StreamWriter(torrcPath))
                {
                    foreach (string content in torrcContent)
                        outputFile.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex.Message);
                throw new Exception(ex.Message);
            }

            logger.LogInformation($"Created torrc file: {torrcPath}");
        }

        public void Dispose()
        {
            TorProcess?.Kill();
        }

    }
}
