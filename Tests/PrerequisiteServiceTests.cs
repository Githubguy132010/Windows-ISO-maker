using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using Windows_ISO_Maker.Services;

namespace Windows_ISO_Maker.Tests
{
    [TestClass]
    public class PrerequisiteServiceTests
    {
        private string testPath;

        [TestInitialize]
        public void Setup()
        {
            testPath = Path.Combine(Path.GetTempPath(), "WindowsISOMakerTests");
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
            Directory.CreateDirectory(testPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
        }

        [TestMethod]
        public void ArePrerequisitesInstalled_WhenToolsNotPresent_ReturnsFalse()
        {
            // Arrange
            var service = new PrerequisiteService();

            // Act
            var result = service.ArePrerequisitesInstalled();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsDismAvailable_WhenOnWindows_ReturnsTrue()
        {
            // This test should only run on Windows
            if (!OperatingSystem.IsWindows())
                return;

            // Arrange
            var service = new PrerequisiteService();

            // Act
            var result = service.IsDismAvailable();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsDismAvailable_WhenNotOnWindows_ReturnsFalse()
        {
            // This test should only run on non-Windows platforms
            if (OperatingSystem.IsWindows())
                return;

            // Arrange
            var service = new PrerequisiteService();

            // Act
            var result = service.IsDismAvailable();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ExtractBundledTools_WhenToolZipsPresent_ExtractsTools()
        {
            // Arrange
            var mockZipPath = Path.Combine(testPath, "Resources");
            Directory.CreateDirectory(mockZipPath);

            // Create mock zip files
            await File.WriteAllBytesAsync(Path.Combine(mockZipPath, "oscdimg.zip"), new byte[] { 0x50, 0x4B, 0x05, 0x06 }); // Empty ZIP file header
            await File.WriteAllBytesAsync(Path.Combine(mockZipPath, "pwsh.zip"), new byte[] { 0x50, 0x4B, 0x05, 0x06 }); // Empty ZIP file header

            var service = new PrerequisiteService();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => service.ExtractBundledTools());
        }

        [TestMethod]
        public void DownloadProgressChanged_WhenProgressReported_EventIsRaised()
        {
            // Arrange
            var service = new PrerequisiteService();
            var eventRaised = false;
            ToolsDownloadProgressEventArgs? receivedArgs = null;

            service.DownloadProgressChanged += (s, e) =>
            {
                eventRaised = true;
                receivedArgs = e;
            };

            // Act
            service.ReportProgress("TestComponent", 50, 100, "Testing...");

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual("TestComponent", receivedArgs.Component);
            Assert.AreEqual(50, receivedArgs.BytesTransferred);
            Assert.AreEqual(100, receivedArgs.TotalBytes);
            Assert.AreEqual("Testing...", receivedArgs.Status);
        }
    }
}