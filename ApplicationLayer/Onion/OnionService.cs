// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TangramCypher.Helper;

namespace TangramCypher.ApplicationLayer.Onion
{
    public class OnionService : HostedService, IOnionService, IDisposable
    {
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        readonly IConfigurationSection onionSection;
        readonly ILogger logger;
        readonly IConsole console;
        readonly string controlHost;
        readonly string onionDirectory;
        readonly string torrcPath;
        readonly string controlPortPath;
        readonly string hiddenServicePath;
        readonly string hiddenServicePort;
        readonly string obfs4proxy;
        string hashedPassword;
        int torId = 0;

        Process TorProcess { get; set; }
        public bool OnionStarted { get; private set; }
        public string SocksHost { get; }
        public int SocksPort { get; }
        public int OnionEnabled { get; }
        public int ControlPort { get; }

        public OnionService(IConfiguration configuration, ILogger logger, IConsole console)
        {
            onionSection = configuration.GetSection(Constants.ONION);

            this.logger = logger;
            this.console = console;

            SocksHost = onionSection.GetValue<string>(Constants.SOCKS_HOST);
            SocksPort = onionSection.GetValue<int>(Constants.SOCKS_PORT);
            controlHost = onionSection.GetValue<string>(Constants.CONTROL_HOST);
            ControlPort = onionSection.GetValue<int>(Constants.CONTROL_PORT);
            hiddenServicePort = onionSection.GetValue<string>(Constants.HIDDEN_SERVICE_PORT);
            OnionEnabled = onionSection.GetValue<int>(Constants.ONION_ENABLED);

            onionDirectory = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), Constants.ONION);
            torrcPath = Path.Combine(onionDirectory, Constants.TORRC);
            controlPortPath = Path.Combine(onionDirectory, "control-port");
            hiddenServicePath = Path.Combine(onionDirectory, "hidden_service");
            var pluggableTransports = Path.Combine(tangramDirectory.FullName, "PluggableTransports");
            obfs4proxy = Path.Combine(pluggableTransports, "obfs4proxy");
        }

        public void ChangeCircuit(SecureString password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, ControlPort, insecurePassword.Value);
                    controlPortClient.ChangeCircuitAsync().Wait();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
            }
        }

        public void DisconnectDisposeSocket(SecureString password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, ControlPort, insecurePassword.Value);
                    controlPortClient.DisconnectDisposeSocket();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
            }
        }

        public void InitializeConnectSocket(SecureString password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, ControlPort, insecurePassword.Value);
                    controlPortClient.InitializeConnectSocketAsync(new CancellationToken()).Wait();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
            }
        }

        public async Task<bool> CircuitEstablished(SecureString password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            bool hasCircuit = false;

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, ControlPort, insecurePassword.Value);
                    hasCircuit = await controlPortClient.IsCircuitEstablishedAsync();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
            }

            return hasCircuit;
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
                throw new ArgumentException("Command cannot be null or empty!", nameof(command));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, ControlPort, insecurePassword.Value);
                    var result = controlPortClient.SendCommandAsync(command).GetAwaiter().GetResult();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                console.WriteLine(ex.Message);
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

            GenerateHashPassword("ILoveTangram".ToSecureString());

            var torrcContent = new string[] {
                "AvoidDiskWrites 1",
                $"HashedControlPassword {hashedPassword}",
                "CircuitBuildTimeout 2",
                "KeepalivePeriod 2",
                "NewCircuitPeriod 15",
                "NumEntryGuards 8",
                $"ControlPort {ControlPort}",
                "Log notice stdout",
                "SafeSocks 1",
                "TestSocks 1",
                $"DataDirectory {onionDirectory}",
                $"ControlPortWriteToFile {controlPortPath}",
                $"ClientTransportPlugin obfs4 exec {obfs4proxy}"
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
                catch { }
            }

            return port == 0 ? ControlPort : port;
        }

        async Task StartTorProcess()
        {
            if (torId.Equals(0))
            {
                TorProcess = new Process();
                TorProcess.StartInfo.FileName = GetTorFileName();
                TorProcess.StartInfo.Arguments = $"-f \"{torrcPath}\"";
                TorProcess.StartInfo.UseShellExecute = false;
                TorProcess.StartInfo.CreateNoWindow = true;
                TorProcess.StartInfo.RedirectStandardOutput = true;
                TorProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        if (e.Data.Contains("Bootstrapped 100%: Done"))
                        {
                            OnionStarted = true;
                            console.ResetColor();
                            logger.LogInformation("tor Started!");
                        }

                        logger.LogInformation(e.Data);
                    }
                };

                TorProcess.Start();

                torId = TorProcess.Id;

                await Task.Run(() => TorProcess.BeginOutputReadLine());
            }

        }

        public override void Dispose()
        {
            try
            {
                foreach (Process tor in Process.GetProcesses())
                {
                    if (tor.Id == torId)
                    {
                        tor.Kill();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("EXECUTE ASYNC ONION");

            if (OnionEnabled == 1)
            {
                console.WriteLine("Starting Onion Service");
                logger.LogInformation("Starting Onion Service");
                await Task.Run(() => StartOnion());
            }

            return;
        }
    }
}
