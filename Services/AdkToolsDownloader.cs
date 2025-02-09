using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Windows_ISO_Maker.Services
{
    public class AdkToolsDownloader
    {
        private const string ADK_DOWNLOAD_URL = "https://go.microsoft.com/fwlink/?linkid=2196127"; // Windows 11 ADK
        private const string OSCDIMG_RELATIVE_PATH = @"Windows Preinstallation Environment\amd64\oscdimg";

        public static async Task<string> DownloadAndExtractOscdimg(string targetDirectory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("This tool requires Windows to run.");

            string adkSetupPath = Path.Combine(Path.GetTempPath(), "adksetup.exe");
            string oscdimgDir = Path.Combine(targetDirectory, "oscdimg");
            Directory.CreateDirectory(oscdimgDir);

            using (var client = new HttpClient())
            {
                // Download ADK setup
                await client.DownloadFileAsync(ADK_DOWNLOAD_URL, adkSetupPath);

                // Run ADK setup to download only the Deployment Tools
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = adkSetupPath;
                    process.StartInfo.Arguments = $"/quiet /layout \"{Path.GetTempPath()}\\ADKLayout\" /features OptionId.DeploymentTools";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                        throw new Exception("Failed to download ADK components");
                }

                // Extract oscdimg from the layout
                string layoutPath = Path.Combine(Path.GetTempPath(), "ADKLayout", OSCDIMG_RELATIVE_PATH);
                if (Directory.Exists(layoutPath))
                {
                    foreach (var file in Directory.GetFiles(layoutPath))
                    {
                        File.Copy(file, Path.Combine(oscdimgDir, Path.GetFileName(file)), true);
                    }
                }
                else
                {
                    throw new Exception("Failed to locate oscdimg in the downloaded ADK components");
                }
            }

            try
            {
                File.Delete(adkSetupPath);
                Directory.Delete(Path.Combine(Path.GetTempPath(), "ADKLayout"), true);
            }
            catch { /* Ignore cleanup errors */ }

            return oscdimgDir;
        }
    }
}