using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Vault;

namespace TangramCypher.ApplicationLayer.Vault
{
    public class VaultService : IVaultService
    {
        private static readonly DirectoryInfo userDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(Path.Combine(userDirectory.FullName, ".tangramcli"));

        private FileInfo vaultExecutable;
        private Process vaultProcess;

        public VaultService()
        {

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

            if(fileInfo != null && fileInfo.Length == 1)
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

                PhysicalConsole.Singleton.ResetColor();
                PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.Yellow;
                PhysicalConsole.Singleton.WriteLine("Warning: Existing Vault Process Detected.");
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
        }

        public void Unseal(string shard)
        {
            var vaultOptions = VaultOptions.Default;

            //  TODO: Pull this from settings file.
            vaultOptions.Address = "http://127.0.0.1:8200";

            var vaultClient = new VaultClient(vaultOptions);

            var unsealTask = vaultClient.Sys.Unseal(shard);
            unsealTask.Wait();

            var response = unsealTask.Result;

            if (!response.Sealed)
            {
                PhysicalConsole.Singleton.ResetColor();
                PhysicalConsole.Singleton.ForegroundColor = ConsoleColor.DarkGreen;
                PhysicalConsole.Singleton.WriteLine("Vault Unsealed!");
            }
        }
    }
}
