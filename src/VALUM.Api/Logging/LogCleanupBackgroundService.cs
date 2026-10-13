using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace VALUM.Api.Logging
{
    public class LogCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<LogCleanupBackgroundService> _logger;
        private readonly LogCleanupOptions _options;
        private readonly string _logDirectory;

        public LogCleanupBackgroundService(
            ILogger<LogCleanupBackgroundService> logger,
            IOptions<LogCleanupOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            // Use content root path to ensure consistency with Serilog's relative path
            // When running from solution root, both Serilog and cleanup will use the same directory
            var contentRoot = Directory.GetCurrentDirectory();
            _logDirectory = Path.Combine(contentRoot, "logs");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Log cleanup is disabled");
                return;
            }

            // Run initial cleanup on startup (awaited, not fire-and-forget)
            await RunCleanupAsync();

            // Schedule periodic cleanup with sequential execution
            var interval = TimeSpan.FromMinutes(_options.IntervalMinutes);
            using var timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCleanupAsync();
            }
        }

        private Task RunCleanupAsync()
        {
            try
            {
                var algorithm = new LogCleanupAlgorithm(_logDirectory, _options);
                var result = algorithm.ExecuteCleanup();

                if (result.FilesDeleted > 0)
                {
                    _logger.LogInformation(
                        "Log cleanup completed: {FilesDeleted} files deleted, {BytesFreed} bytes freed, {RemainingSizeBytes} bytes remaining",
                        result.FilesDeleted,
                        result.BytesFreed,
                        result.RemainingSizeBytes);

                    if (!result.SizeTargetAchieved)
                    {
                        _logger.LogWarning(
                            "Log cleanup could not achieve size target of {MaxTotalSizeMB} MB. Current size: {RemainingSizeMB} MB",
                            _options.MaxTotalSizeMB,
                            result.RemainingSizeBytes / (1024.0 * 1024.0));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log cleanup failed");
            }

            return Task.CompletedTask;
        }
    }
}
