using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using Windows_ISO_Maker.Services;

namespace Windows_ISO_Maker.Tests
{
    [TestClass]
    public class IsoServiceTests
    {
        private Mock<PrerequisiteService> _mockPrerequisiteService;
        private IsoService _isoService;
        private string _testPath;

        [TestInitialize]
        public void Setup()
        {
            _testPath = Path.Combine(Path.GetTempPath(), "WindowsISOMakerTests");
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
            Directory.CreateDirectory(_testPath);

            _mockPrerequisiteService = new Mock<PrerequisiteService>();
            _isoService = new IsoService(_mockPrerequisiteService.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }

        [TestMethod]
        public async Task CustomizeIso_WhenPrerequisitesNotInstalled_TriesToExtractTools()
        {
            // Arrange
            _mockPrerequisiteService.Setup(x => x.ArePrerequisitesInstalled()).Returns(false);
            _mockPrerequisiteService.Setup(x => x.ExtractBundledTools()).Returns(Task.CompletedTask);
            _mockPrerequisiteService.Setup(x => x.IsDismAvailable()).Returns(true);

            // Act
            await _isoService.CustomizeIso("test.iso", null, false, false);

            // Assert
            _mockPrerequisiteService.Verify(x => x.ExtractBundledTools(), Times.Once);
        }

        [TestMethod]
        public async Task CustomizeIso_WhenDismNotAvailable_ReturnsError()
        {
            // Arrange
            _mockPrerequisiteService.Setup(x => x.ArePrerequisitesInstalled()).Returns(true);
            _mockPrerequisiteService.Setup(x => x.IsDismAvailable()).Returns(false);

            // Act
            var (success, message) = await _isoService.CustomizeIso("test.iso", null, false, false);

            // Assert
            Assert.IsFalse(success);
            Assert.IsTrue(message.Contains("DISM is not available"));
        }

        [TestMethod]
        public void ProgressChanged_WhenProgressReported_EventIsRaised()
        {
            // Arrange
            var eventRaised = false;
            IsoProgressEventArgs? receivedArgs = null;

            _isoService.ProgressChanged += (s, e) =>
            {
                eventRaised = true;
                receivedArgs = e;
            };

            // Act
            _isoService.GetType()
                .GetMethod("ReportProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_isoService, new object[] { "Testing...", 50, false });

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual("Testing...", receivedArgs.Status);
            Assert.AreEqual(50, receivedArgs.ProgressPercentage);
            Assert.IsFalse(receivedArgs.IsIndeterminate);
        }
    }
}