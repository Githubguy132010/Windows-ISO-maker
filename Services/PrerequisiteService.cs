using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace Windows_ISO_Maker.Services
{
    public class PrerequisiteService : IToolsDownloadProgress
    {
        private readonly string _toolsPath;
        private readonly string _oscdimgPath;
        private readonly string _pwshPath;
        private readonly string _resourcesPath;

        public event EventHandler<ToolsDownloadProgressEventArgs>? DownloadProgressChanged;

        public PrerequisiteService()
        {
            _toolsPath = Path.Combine(AppContext.BaseDirectory, "Tools");
            _oscdimgPath = Path.Combine(_toolsPath, "oscdimg", "oscdimg.exe");
            _pwshPath = Path.Combine(_toolsPath, "pwsh", "pwsh.exe");
            _resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources");
            
            EnsureToolsDirectoryExists();
        }

        private void EnsureToolsDirectoryExists()
        {
            Directory.CreateDirectory(_toolsPath);
            Directory.CreateDirectory(Path.Combine(_toolsPath, "oscdimg"));
            Directory.CreateDirectory(Path.Combine(_toolsPath, "pwsh"));
            Directory.CreateDirectory(_resourcesPath);
        }

        public string GetOscdimgPath() => _oscdimgPath;
        public string GetPwshPath() => _pwshPath;

        public bool ArePrerequisitesInstalled()
        {
            return File.Exists(_oscdimgPath) && File.Exists(_pwshPath);
        }

        private bool AreToolZipsPresent()
        {
            return File.Exists(Path.Combine(_resourcesPath, "oscdimg.zip")) &&
                   File.Exists(Path.Combine(_resourcesPath, "pwsh.zip"));
        }

        public async Task ExtractBundledTools()
        {
            if (!AreToolZipsPresent())
            {
                await DownloadAndPackageTools();
            }

            // Extract from embedded resources or local files
            var resourcePaths = new[] { "oscdimg.zip", "pwsh.zip" };
            foreach (var resourceName in resourcePaths)
            {
                string zipPath = Path.Combine(_resourcesPath, resourceName);
                if (File.Exists(zipPath))
                {
                    string targetDir = resourceName.Contains("oscdimg")
                        ? Path.Combine(_toolsPath, "oscdimg")
                        : Path.Combine(_toolsPath, "pwsh");

                    ZipFile.ExtractToDirectory(zipPath, targetDir, true);
                }
            }
        }

        public void ReportProgress(string component, long bytesTransferred, long? totalBytes, string status)
        {
            DownloadProgressChanged?.Invoke(this, new ToolsDownloadProgressEventArgs(component, bytesTransferred, totalBytes, status));
        }

        private async Task DownloadFileWithProgressAsync(HttpClient client, string url, string filename, string component)
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            
            var buffer = new byte[81920];
            long bytesTransferred = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                bytesTransferred += bytesRead;
                ReportProgress(component, bytesTransferred, totalBytes, $"Downloading {component}...");
            }
        }

        private async Task DownloadAndPackageTools()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WindowsISOMakerTools");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            try
            {
                // Download PowerShell
                using (var client = new HttpClient())
                {
                    string pwshZipPath = Path.Combine(tempDir, "pwsh-full.zip");
                    var pwshUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.3.6/PowerShell-7.3.6-win-x64.zip";
                    
                    await DownloadFileWithProgressAsync(client, pwshUrl, pwshZipPath, "PowerShell");
                    ReportProgress("PowerShell", 100, 100, "Extracting PowerShell...");
                    
                    // Extract full PowerShell
                    string pwshExtractPath = Path.Combine(tempDir, "pwsh-full");
                    ZipFile.ExtractToDirectory(pwshZipPath, pwshExtractPath);

                    // Create minimal PowerShell package
                    string pwshMinimalPath = Path.Combine(tempDir, "pwsh-minimal");
                    Directory.CreateDirectory(pwshMinimalPath);

                    var requiredFiles = new[]
                    {
                        "pwsh.exe",
                        "pwsh.dll",
                        "System.Management.Automation.dll",
                        "Microsoft.PowerShell.Commands.Management.dll",
                        "Microsoft.PowerShell.Commands.Utility.dll"
                    };

                    foreach (var file in requiredFiles)
                    {
                        File.Copy(
                            Path.Combine(pwshExtractPath, file),
                            Path.Combine(pwshMinimalPath, file),
                            true);
                    }

                    // Create minimal PowerShell zip
                    string pwshZipOutput = Path.Combine(_resourcesPath, "pwsh.zip");
                    if (File.Exists(pwshZipOutput))
                        File.Delete(pwshZipOutput);
                    ZipFile.CreateFromDirectory(pwshMinimalPath, pwshZipOutput);
                }

                // Try to find Windows ADK installation first
                var adkPaths = new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg",
                    @"C:\Program Files (x86)\Windows Kits\11\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg"
                };

                string? adkPath = adkPaths.FirstOrDefault(Directory.Exists);
                string oscdimgDir;
                
                if (adkPath != null)
                {
                    // Use local ADK installation
                    oscdimgDir = Path.Combine(tempDir, "oscdimg");
                    Directory.CreateDirectory(oscdimgDir);

                    foreach (var file in Directory.GetFiles(adkPath))
                    {
                        File.Copy(file, Path.Combine(oscdimgDir, Path.GetFileName(file)), true);
                    }
                }
                else
                {
                    // Download oscdimg if ADK is not installed
                    try
                    {
                        oscdimgDir = await AdkToolsDownloader.DownloadAndExtractOscdimg(tempDir);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Could not download oscdimg tools: {ex.Message}\n\nPlease install Windows ADK manually from:\nhttps://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install");
                    }
                }

                // Create oscdimg zip
                string oscdimgZipOutput = Path.Combine(_resourcesPath, "oscdimg.zip");
                if (File.Exists(oscdimgZipOutput))
                    File.Delete(oscdimgZipOutput);
                ZipFile.CreateFromDirectory(oscdimgDir, oscdimgZipOutput);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        public bool IsDismAvailable()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dism",
                        Arguments = "/?",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task DownloadFileAsync(this HttpClient client, string url, string filename)
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream);
        }
    }
}