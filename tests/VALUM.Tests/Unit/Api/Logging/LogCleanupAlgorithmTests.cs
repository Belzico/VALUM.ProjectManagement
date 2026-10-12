using System;
using System.IO;
using VALUM.Api.Logging;
using Xunit;

namespace VALUM.Tests.Unit.Api.Logging
{
    public class LogCleanupAlgorithmTests : IDisposable
    {
        private readonly string _testLogPath;

        public LogCleanupAlgorithmTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), "valum_cleanup_test_" + Guid.NewGuid());
            Directory.CreateDirectory(_testLogPath);
        }

        [Fact]
        public void EnabledFalse_NoFilesDeleted()
        {
            var options = new LogCleanupOptions
            {
                Enabled = false,
                MaxAgeDays = 0,
                MaxTotalSizeMB = 0,
                PreventDeletion = false
            };

            // Create some test files
            File.WriteAllText(Path.Combine(_testLogPath, "valum-20260101.log"), "old log");
            File.WriteAllText(Path.Combine(_testLogPath, "valum-20260102.log"), "new log");

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            Assert.Equal(0, result.FilesDeleted);
            Assert.Equal(2, Directory.GetFiles(_testLogPath).Length);
        }

        [Fact]
        public void PreventDeletionTrue_NoFilesDeleted()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 0,
                MaxTotalSizeMB = 0,
                PreventDeletion = true
            };

            // Create old test file
            var oldFile = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(oldFile, "old log");
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-30));

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            Assert.Equal(0, result.FilesDeleted);
            Assert.True(File.Exists(oldFile));
        }

        [Fact]
        public void FilesOlderThanMaxAgeDays_AreDeleted()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 7,
                MaxTotalSizeMB = 1000,
                PreventDeletion = false
            };

            // Create old test file
            var oldFile = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(oldFile, "old log");
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-10));

            // Create new test file
            var newFile = Path.Combine(_testLogPath, "valum-20260110.log");
            File.WriteAllText(newFile, "new log");
            File.SetLastWriteTimeUtc(newFile, DateTime.UtcNow.AddDays(-1));

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            Assert.Equal(1, result.FilesDeleted);
            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(newFile));
        }

        [Fact]
        public void FilesWithinAgeLimit_ArePreserved()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 7,
                MaxTotalSizeMB = 1000,
                PreventDeletion = false
            };

            // Create recent test file
            var recentFile = Path.Combine(_testLogPath, "valum-20260110.log");
            File.WriteAllText(recentFile, "recent log");
            File.SetLastWriteTimeUtc(recentFile, DateTime.UtcNow.AddDays(-1));

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            Assert.Equal(0, result.FilesDeleted);
            Assert.True(File.Exists(recentFile));
        }

        [Fact]
        public void UnrelatedFilenames_AreNeverDeleted()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 0,
                MaxTotalSizeMB = 0,
                PreventDeletion = false
            };

            // Create unrelated file
            var unrelatedFile = Path.Combine(_testLogPath, "other.txt");
            File.WriteAllText(unrelatedFile, "unrelated");
            File.SetLastWriteTimeUtc(unrelatedFile, DateTime.UtcNow.AddDays(-30));

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            Assert.Equal(0, result.FilesDeleted);
            Assert.True(File.Exists(unrelatedFile));
        }

        [Fact]
        public void EmptyDirectory_HandledSafely()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 0,
                MaxTotalSizeMB = 0,
                PreventDeletion = false
            };

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            Assert.Equal(0, result.FilesDeleted);
        }

        [Fact]
        public void WhenSizeExceedsMaxTotalSizeMB_DeletesOldestFilesFirst_EvenIfWithinAgeLimit()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 100, // Large age limit, PASS 1 won't delete
                MaxTotalSizeMB = 1, // 1 MB limit
                PreventDeletion = false
            };

            // Create files totalling > 1 MB (all within age limit but clearly NOT current day)
            var yesterday = DateTime.UtcNow.AddDays(-1);
            var file1 = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(file1, new string('A', 800 * 1024)); // 800 KB
            File.SetLastWriteTimeUtc(file1, yesterday.AddHours(-2));

            var file2 = Path.Combine(_testLogPath, "valum-20260102.log");
            File.WriteAllText(file2, new string('B', 500 * 1024)); // 500 KB
            File.SetLastWriteTimeUtc(file2, yesterday.AddHours(-1));

            var file3 = Path.Combine(_testLogPath, "valum-20260103.log");
            File.WriteAllText(file3, new string('C', 200 * 1024)); // 200 KB (most recent)
            File.SetLastWriteTimeUtc(file3, yesterday);

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            // PASS 2 size-based cleanup should delete oldest files
            // Total is 1.5 MB, need to delete at least 500 KB to get under 1 MB
            Assert.True(result.FilesDeleted > 0, $"Expected files deleted by PASS 2, got {result.FilesDeleted}");
            // PASS 2 should have deleted something (file1 or file2 or both)
            // since they're not current-day files
            Assert.True(!File.Exists(file1) || !File.Exists(file2), "At least one old file should be deleted by PASS 2");
        }

        [Fact]
        public void WhenOnlyCurrentDayFilesExceedSize_PreservesThemAndReportsUnmetTarget()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 7,
                MaxTotalSizeMB = 1, // 1 MB limit
                PreventDeletion = false
            };

            // Create current-day files totalling > 1 MB (all protected by current-day status)
            var file1 = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(file1, new string('A', 600 * 1024));
            File.SetLastWriteTimeUtc(file1, DateTime.UtcNow);

            var file2 = Path.Combine(_testLogPath, "valum-20260102.log");
            File.WriteAllText(file2, new string('B', 600 * 1024));
            File.SetLastWriteTimeUtc(file2, DateTime.UtcNow);

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            // No files should be deleted (all protected by current-day status)
            Assert.Equal(0, result.FilesDeleted);
            Assert.False(result.SizeTargetAchieved);
            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
        }

        [Fact]
        public void LogCleanupOptions_WithNegativeIntervalMinutes_UsesDefault()
        {
            var options = new LogCleanupOptions
            {
                IntervalMinutes = -1
            };

            Assert.Equal(60, options.IntervalMinutes);
        }

        [Fact]
        public void LogCleanupOptions_WithNegativeMaxAgeDays_UsesDefault()
        {
            var options = new LogCleanupOptions
            {
                MaxAgeDays = -1
            };

            Assert.Equal(7, options.MaxAgeDays);
        }

        [Fact]
        public void LogCleanupOptions_WithNegativeMaxTotalSizeMB_UsesDefault()
        {
            var options = new LogCleanupOptions
            {
                MaxTotalSizeMB = -1
            };

            Assert.Equal(100, options.MaxTotalSizeMB);
        }

        [Fact]
        public void NewestLogFile_IsProtectedDuringSizeBasedCleanup()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 100, // Large age limit, PASS 1 won't delete
                MaxTotalSizeMB = 1, // 1 MB limit
                PreventDeletion = false
            };

            // Create files where newest is from yesterday (but should still be protected)
            var yesterday = DateTime.UtcNow.AddDays(-1);
            var file1 = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(file1, new string('A', 800 * 1024)); // 800 KB
            File.SetLastWriteTimeUtc(file1, yesterday.AddHours(-2));

            var file2 = Path.Combine(_testLogPath, "valum-20260102.log");
            File.WriteAllText(file2, new string('B', 500 * 1024)); // 500 KB (newest)
            File.SetLastWriteTimeUtc(file2, yesterday.AddHours(-1));

            var file3 = Path.Combine(_testLogPath, "valum-20260103.log");
            File.WriteAllText(file3, new string('C', 200 * 1024)); // 200 KB
            File.SetLastWriteTimeUtc(file3, yesterday.AddHours(-3));

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            // Newest file (file2) should be protected even though not current-day
            Assert.True(File.Exists(file2), "Newest file should be protected");
            // File1 (oldest) should be deleted
            Assert.False(File.Exists(file1), "Oldest file should be deleted");
        }

        [Fact]
        public void LockedFile_DoesNotCrashCleanup()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 0,
                MaxTotalSizeMB = 1,
                PreventDeletion = false
            };

            var file1 = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(file1, new string('A', 100 * 1024));
            File.SetLastWriteTimeUtc(file1, DateTime.UtcNow.AddDays(-10));

            // Lock the file
            using (var stream = File.Open(file1, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
                var result = algorithm.ExecuteCleanup();

                // Should not crash, locked file should be skipped
                Assert.True(File.Exists(file1), "Locked file should be preserved");
            }
        }

        [Fact]
        public void NewestFile_IsProtectedInBothCleanupPasses_EvenIfOlderThanMaxAgeDays()
        {
            var options = new LogCleanupOptions
            {
                Enabled = true,
                MaxAgeDays = 1, // 1 day age limit
                MaxTotalSizeMB = 1000, // Large size limit (PASS 2 won't trigger)
                PreventDeletion = false
            };

            // Newest file is 2 days old (older than MaxAgeDays)
            var file1 = Path.Combine(_testLogPath, "valum-20260101.log");
            File.WriteAllText(file1, new string('A', 100 * 1024)); // 100 KB
            File.SetLastWriteTimeUtc(file1, DateTime.UtcNow.AddDays(-2));

            // Older file is 4 days old
            var file2 = Path.Combine(_testLogPath, "valum-20260102.log");
            File.WriteAllText(file2, new string('B', 100 * 1024)); // 100 KB
            File.SetLastWriteTimeUtc(file2, DateTime.UtcNow.AddDays(-4));

            var algorithm = new LogCleanupAlgorithm(_testLogPath, options);
            var result = algorithm.ExecuteCleanup();

            // Newest file (file1) should be preserved in BOTH passes even though > MaxAgeDays
            Assert.True(File.Exists(file1), "Newest file should be preserved even if older than MaxAgeDays");
            // Older file (file2) should be deleted by PASS 1
            Assert.False(File.Exists(file2), "Older completed file should be deleted");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testLogPath))
                {
                    Directory.Delete(_testLogPath, true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
