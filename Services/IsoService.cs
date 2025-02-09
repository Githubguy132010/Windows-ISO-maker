using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;

namespace Windows_ISO_Maker.Services
{
    public class IsoService
    {
        private readonly PrerequisiteService _prerequisiteService;
        public event EventHandler<IsoProgressEventArgs>? ProgressChanged;

        public IsoService(PrerequisiteService prerequisiteService)
        {
            _prerequisiteService = prerequisiteService;
        }

        private void ReportProgress(string status, int? progressPercentage = null, bool isIndeterminate = true)
        {
            ProgressChanged?.Invoke(this, new IsoProgressEventArgs(status, progressPercentage, isIndeterminate));
        }

        public async Task<(bool success, string message)> CustomizeIso(string isoPath, string driversPath, bool enableWinRE, bool keepOriginalOptions)
        {
            try
            {
                ReportProgress("Checking prerequisites...");
                if (!_prerequisiteService.ArePrerequisitesInstalled())
                {
                    ReportProgress("Setting up required tools...");
                    try
                    {
                        await _prerequisiteService.ExtractBundledTools();
                    }
                    catch (Exception ex) when (ex.Message.Contains("Windows ADK"))
                    {
                        return (false, "Windows ADK is required. Please install it from:\nhttps://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install\n\nAfter installation, restart the application.");
                    }
                }

                if (!_prerequisiteService.IsDismAvailable())
                {
                    return (false, "DISM is not available on this system. This tool requires Windows 10 or later.");
                }

                ReportProgress("Preparing workspace...", 5);
                string workDir = Path.Combine(Path.GetTempPath(), "WindowsISOCustomization");
                string mountDir = Path.Combine(workDir, "Mount");
                string extractDir = Path.Combine(workDir, "Extract");
                string outputPath = Path.Combine(
                    Path.GetDirectoryName(isoPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Custom_{Path.GetFileName(isoPath)}");

                if (Directory.Exists(workDir))
                    Directory.Delete(workDir, true);

                Directory.CreateDirectory(workDir);
                Directory.CreateDirectory(mountDir);
                Directory.CreateDirectory(extractDir);

                // Mount ISO and extract contents
                ReportProgress("Mounting ISO image...", 10);
                var pwshPath = _prerequisiteService.GetPwshPath();
                using (var process = new Process())
                {
                    process.StartInfo.FileName = pwshPath;
                    process.StartInfo.Arguments = $"-Command \"Mount-DiskImage -ImagePath '{isoPath}' -PassThru | Get-DiskImage | Get-Volume | Select-Object -ExpandProperty DriveLetter\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    
                    await Task.Run(() => process.Start());
                    string driveLetter = (await process.StandardOutput.ReadToEndAsync()).Trim() + ":\\";
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        ReportProgress("Copying ISO contents...", 20);
                        Directory.CreateDirectory(extractDir);
                        await Task.Run(() => CopyDirectory(driveLetter, extractDir, (progress) =>
                        {
                            ReportProgress("Copying ISO contents...", 20 + (int)(progress * 0.2));
                        }));

                        ReportProgress("Unmounting ISO...", 40);
                        process.StartInfo.Arguments = $"-Command \"Dismount-DiskImage -ImagePath '{isoPath}'\"";
                        await Task.Run(() => process.Start());
                        await process.WaitForExitAsync();
                    }
                    else
                    {
                        throw new Exception("Failed to mount ISO file");
                    }
                }

                // Mount Windows image
                ReportProgress("Locating Windows image...", 45);
                string installWim = Path.Combine(extractDir, "sources", "install.wim");
                if (!File.Exists(installWim))
                    installWim = Path.Combine(extractDir, "sources", "install.esd");

                if (!File.Exists(installWim))
                    throw new FileNotFoundException("Windows image file not found in ISO");

                ReportProgress("Mounting Windows image...", 50);
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "dism";
                    process.StartInfo.Arguments = $"/Mount-Image /ImageFile:\"{installWim}\" /Index:1 /MountDir:\"{mountDir}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    await Task.Run(() => process.Start());
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                        throw new Exception("Failed to mount Windows image");
                }

                if (!string.IsNullOrEmpty(driversPath))
                {
                    ReportProgress("Adding drivers...", 60);
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = "dism";
                        process.StartInfo.Arguments = $"/Image:\"{mountDir}\" /Add-Driver /Driver:\"{driversPath}\" /Recurse";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        await Task.Run(() => process.Start());
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                            throw new Exception("Failed to add drivers");
                    }
                }

                if (enableWinRE)
                {
                    ReportProgress("Enabling Windows Recovery Environment...", 70);
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = "dism";
                        process.StartInfo.Arguments = $"/Image:\"{mountDir}\" /Enable-Feature /FeatureName:WinRE-Features";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        await Task.Run(() => process.Start());
                        await process.WaitForExitAsync();
                    }
                }

                ReportProgress("Saving changes and unmounting image...", 80);
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "dism";
                    process.StartInfo.Arguments = $"/Unmount-Image /MountDir:\"{mountDir}\" /Commit";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    await Task.Run(() => process.Start());
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                        throw new Exception("Failed to unmount Windows image");
                }

                ReportProgress("Creating new ISO...", 90);
                string oscdimgPath = _prerequisiteService.GetOscdimgPath();
                if (File.Exists(oscdimgPath))
                {
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = oscdimgPath;
                        process.StartInfo.Arguments = $"-m -o -u2 -udfver102 -bootdata:2#p0,e,b\"{extractDir}\\boot\\etfsboot.com\"#pEF,e,b\"{extractDir}\\efi\\microsoft\\boot\\efisys.bin\" \"{extractDir}\" \"{outputPath}\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        await Task.Run(() => process.Start());
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                            throw new Exception("Failed to create new ISO");
                    }
                }
                else
                {
                    throw new FileNotFoundException("Required tools are missing. Please restart the application.");
                }

                ReportProgress("Cleaning up...", 95);
                try
                {
                    if (Directory.Exists(workDir))
                        Directory.Delete(workDir, true);
                }
                catch { /* Ignore cleanup errors */ }

                ReportProgress("Completed", 100, false);
                return (true, $"ISO customization completed. Output saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Windows ADK"))
                {
                    return (false, "Windows ADK is required. Please install it from:\nhttps://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install");
                }
                return (false, $"Error: {ex.Message}");
            }
        }

        private async Task CopyDirectory(string sourceDir, string destinationDir, Action<double> progressCallback)
        {
            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            var copiedFiles = 0;

            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            foreach (string filePath in files)
            {
                string destPath = filePath.Replace(sourceDir, destinationDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(filePath, destPath, true);
                copiedFiles++;
                progressCallback((double)copiedFiles / totalFiles);
                await Task.Delay(1); // Allow UI updates
            }
        }
    }
}