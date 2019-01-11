using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer;
using TangramCypher.Helpers;
using TangramCypher.Helpers.LibSodium;

namespace Cypher.ApplicationLayer.Onion
{
    public class OnionService : HostedService, IOnionService, IDisposable
    {
        const string ONION = "onion";
        const string TORRC = "torrc";
        const string SOCKS_HOST = "onion_socks_host";
        const string SOCKS_PORT = "onion_socks_port";
        const string CONTROL_HOST = "onion_control_host";
        const string CONTROL_PORT = "onion_control_port";
        const string HASHED_CONTROL_PASSWORD = "onion_hashed_control_password";
        const string HIDDEN_SERVICE_PORT = "onion_hidden_service_port";
        const string ONION_ENABLED = "onion_enabled";

        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        readonly ICryptography cryptography;
        readonly IConfigurationSection onionSection;
        readonly ILogger logger;
        readonly IConsole console;
        readonly string socksHost;
        readonly int socksPort;
        readonly string controlHost;
        readonly int controlPort;
        readonly string onionDirectory;
        readonly string torrcPath;
        readonly string controlPortPath;
        readonly string hiddenServicePath;
        readonly string hiddenServicePort;
        readonly int onionEnabled;

        string hashedPassword;

        Process TorProcess { get; set; }

        public bool OnionStarted { get; private set; }

        public OnionService(ICryptography cryptography, IConfiguration configuration, ILogger logger, IConsole console)
        {
            this.cryptography = cryptography;
            onionSection = configuration.GetSection(ONION);

            this.logger = logger;
            this.console = console;

            socksHost = onionSection.GetValue<string>(SOCKS_HOST);
            socksPort = onionSection.GetValue<int>(SOCKS_PORT);
            controlHost = onionSection.GetValue<string>(CONTROL_HOST);
            controlPort = onionSection.GetValue<int>(CONTROL_PORT);
            hiddenServicePort = onionSection.GetValue<string>(HIDDEN_SERVICE_PORT);
            onionEnabled = onionSection.GetValue<int>(ONION_ENABLED);

            onionDirectory = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), ONION);
            torrcPath = Path.Combine(onionDirectory, TORRC);
            controlPortPath = Path.Combine(onionDirectory, "control-port");
            hiddenServicePath = Path.Combine(onionDirectory, "hidden_service");
        }

        public async Task<T> ClientGetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
            {
                throw new ArgumentNullException(nameof(baseAddress));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is missing!", nameof(path));
            }

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                        return Util.DeserializeJsonFromStream<T>(stream);

                    var content = await Util.StreamToStringAsync(stream);
                    throw new ApiException
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = content
                    };
                }
            }
        }

        public void ChangeCircuit(SecureString password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, insecurePassword.Value);
                    controlPortClient.ChangeCircuitAsync().Wait();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
            }
        }

        public void GenerateHashPassword(SecureString password)
        {
            using (var insecurePassword = password.Insecure())
            {
                var torProcessStartInfo = new ProcessStartInfo(GetTorFileName())
                {
                    Arguments = $"--hash-password {password}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                TorProcess = Process.Start(torProcessStartInfo);

                var sOut = TorProcess.StandardOutput;

                var raw = sOut.ReadToEnd();

                var lines = raw.Split(Environment.NewLine);

                string result = string.Empty;

                //  If it's multi-line use the last non-empty line.
                //  We don't want to pull in potential warnings.
                if (lines.Length > 1)
                {
                    var rlines = lines.Reverse();
                    foreach (var line in rlines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            result = Regex.Replace(line, Environment.NewLine, string.Empty);
                            logger.LogInformation($"Hopefully password line: {line}");
                            break;
                        }
                    }
                }

                if (!TorProcess.HasExited)
                {
                    TorProcess.Kill();
                }

                sOut.Close();
                TorProcess.Close();
                TorProcess = null;

                hashedPassword = Regex.Match(result, "16:[0-9A-F]+")?.Value ?? string.Empty;
            }
        }

        public void SendCommands(string command, SecureString password)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("Command cannot be null or empty!", nameof(command));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, insecurePassword.Value);
                    var result = controlPortClient.SendCommandAsync(command).GetAwaiter().GetResult();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
            }
        }

        public async Task<JObject> ClientPostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (!Equals(payload, default(T)))
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (baseAddress == null)
            {
                throw new ArgumentNullException(nameof(baseAddress));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is missing!", nameof(path));
            }

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    var content = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var buffer = Encoding.UTF8.GetBytes(content);

                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var result = Util.DeserializeJsonFromStream<JObject>(stream);
                            return Task.FromResult(result).Result;
                        }

                        var contentResult = await Util.StreamToStringAsync(stream);
                        throw new ApiException
                        {
                            StatusCode = (int)response.StatusCode,
                            Content = contentResult
                        };
                    }
                }
            }
        }

        public void StartOnion()
        {
            OnionStarted = false;

            CreateTorrc();
            StartTorProcess().GetAwaiter();
        }

        static string GetTorFileName()
        {
            var sTor = $"{tangramDirectory}tor";

            if (Util.GetOSPlatform() == OSPlatform.Windows)
                sTor = "tor.exe";

            return sTor;
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
                $"HiddenServiceDir {hiddenServicePath}",
                $"HiddenServicePort {hiddenServicePort}",
                "HiddenServiceVersion 3",
                "CircuitBuildTimeout 10",
                "KeepalivePeriod 60",
                "NumEntryGuards 8",
                "SocksPort 9050",
                "Log notice stdout",
                $"DataDirectory {onionDirectory}",
                $"ControlPortWriteToFile {controlPortPath}"
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

        int ReadControlPort()
        {
            int port = 0;

            if (File.Exists(controlPortPath))
            {
                try
                {
                    int.TryParse(Util.Pop(File.ReadAllText(controlPortPath, Encoding.UTF8), ":"), out port);
                }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
            }

            return port == 0 ? controlPort : port;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async Task StartTorProcess()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            TorProcess = new Process();
            TorProcess.StartInfo.FileName = GetTorFileName();
            TorProcess.StartInfo.Arguments = $"-f \"{torrcPath}\"";
            TorProcess.StartInfo.UseShellExecute = false;
            TorProcess.StartInfo.CreateNoWindow = true;
            TorProcess.StartInfo.RedirectStandardOutput = true;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
            TorProcess.OutputDataReceived += async (sender, e) =>
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.Contains("Bootstrapped 100%: Done"))
                    {
                        OnionStarted = true;
                        console.ResetColor();
                        console.WriteLine("tor Started!");
                        logger.LogInformation("tor Started!");
                    }

                    logger.LogInformation(e.Data);
                }
            };

            TorProcess.Start();
            TorProcess.BeginOutputReadLine();
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public void Dispose()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            try
            {
                TorProcess?.Kill();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable RECS0133 // Parameter name differs in base declaration
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
#pragma warning restore RECS0133 // Parameter name differs in base declaration
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if(onionEnabled == 1)
            {
                console.WriteLine("Starting Onion Service");
                logger.LogInformation("Starting Onion Service");
                GenerateHashPassword("ILoveTangram".ToSecureString());
                StartOnion();
            }
            return;
        }
    }
}
