using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VALUM.Api.Logging;
using Xunit;

namespace VALUM.Tests.Unit.Api.Logging
{
    public class LogCleanupBackgroundServiceTests
    {
        [Fact]
        public void Constructor_WithValidOptions_CreatesService()
        {
            var loggerMock = new Mock<ILogger<LogCleanupBackgroundService>>();
            var options = new LogCleanupOptions
            {
                Enabled = true,
                IntervalMinutes = 60,
                MaxAgeDays = 7,
                MaxTotalSizeMB = 100,
                PreventDeletion = false
            };
            var optionsMock = new Mock<IOptions<LogCleanupOptions>>();
            optionsMock.Setup(x => x.Value).Returns(options);

            var service = new LogCleanupBackgroundService(loggerMock.Object, optionsMock.Object);
            
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WhenDisabled_CreatesService()
        {
            var loggerMock = new Mock<ILogger<LogCleanupBackgroundService>>();
            var options = new LogCleanupOptions
            {
                Enabled = false,
                IntervalMinutes = 60,
                MaxAgeDays = 7,
                MaxTotalSizeMB = 100,
                PreventDeletion = false
            };
            var optionsMock = new Mock<IOptions<LogCleanupOptions>>();
            optionsMock.Setup(x => x.Value).Returns(options);

            var service = new LogCleanupBackgroundService(loggerMock.Object, optionsMock.Object);
            
            Assert.NotNull(service);
        }
    }
}
