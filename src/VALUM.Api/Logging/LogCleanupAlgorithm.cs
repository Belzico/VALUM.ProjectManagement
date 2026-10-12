using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VALUM.Api.Logging
{
    public class LogCleanupAlgorithm
    {
        private readonly string _logDirectory;
        private readonly LogCleanupOptions _options;

        public LogCleanupAlgorithm(string logDirectory, LogCleanupOptions options)
        {
            _logDirectory = logDirectory;
            _options = options;
        }

        public CleanupResult ExecuteCleanup()
        {
            var result = new CleanupResult();

            // If disabled or deletion prevented, do nothing
            if (!_options.Enabled || _options.PreventDeletion)
            {
                return result;
            }

            // If directory doesn't exist, do nothing
            if (!Directory.Exists(_logDirectory))
            {
                return result;
            }

            // Get all VALUM log files (matching valum-*.log pattern)
            var logFiles = Directory.GetFiles(_logDirectory, "valum-*.log")
                .Select(f => new FileInfo(f))
                .ToList();

            if (logFiles.Count == 0)
            {
                return result;
            }

            // PASS 1: AGE-BASED CLEANUP
            // Delete files older than MaxAgeDays, but protect the newest file and current-day files conservatively
            var cutoffDate = DateTime.UtcNow.AddDays(-_options.MaxAgeDays);
            var today = DateTime.UtcNow.Date;
            
            // Identify the newest file before any deletion to protect it
            var newestFile = logFiles.OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            
            foreach (var file in logFiles)
            {
                // Protect current-day files conservatively
                if (file.LastWriteTimeUtc.Date >= today)
                {
                    continue;
                }

                // Protect the newest file (it may still be active even if older than MaxAgeDays)
                if (newestFile != null && file.FullName == newestFile.FullName)
                {
                    continue;
                }

                if (file.LastWriteTimeUtc <= cutoffDate)
                {
                    try
                    {
                        result.BytesFreed += file.Length;
                        file.Delete();
                        result.FilesDeleted++;
                    }
                    catch
                    {
                        // Skip files that can't be deleted (locked, permissions, etc.)
                    }
                }
            }

            // Recalculate remaining files after age-based cleanup
            var remainingFiles = Directory.GetFiles(_logDirectory, "valum-*.log")
                .Select(f => new FileInfo(f))
                .ToList();
            result.RemainingSizeBytes = remainingFiles.Sum(f => f.Length);

            // PASS 2: SIZE-BASED CLEANUP
            // If total size exceeds MaxTotalSizeMB, delete additional files oldest-first
            // regardless of age (except protect current-day files and newest file conservatively)
            var maxSizeBytes = _options.MaxTotalSizeMB * 1024L * 1024L;
            if (result.RemainingSizeBytes > maxSizeBytes)
            {
                // Sort all remaining files by age (oldest first)
                var sortedByAge = remainingFiles.OrderBy(f => f.LastWriteTimeUtc).ToList();
                
                // Re-use today from PASS 1 for consistency
                // Protect current-day files conservatively
                
                // Identify the newest remaining file to protect it (recalculate after PASS 1)
                var newestRemainingFile = sortedByAge.OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
                
                foreach (var file in sortedByAge)
                {
                    if (result.RemainingSizeBytes <= maxSizeBytes)
                    {
                        break;
                    }

                    // Protect current-day files
                    if (file.LastWriteTimeUtc.Date >= today)
                    {
                        continue;
                    }

                    // Protect the newest remaining file (it may still be active even if from previous day)
                    if (newestRemainingFile != null && file.FullName == newestRemainingFile.FullName)
                    {
                        continue;
                    }

                    try
                    {
                        result.BytesFreed += file.Length;
                        file.Delete();
                        result.FilesDeleted++;
                        result.RemainingSizeBytes -= file.Length;
                    }
                    catch
                    {
                        // Skip files that can't be deleted (locked, permissions, etc.)
                    }
                }
            }

            result.SizeTargetAchieved = result.RemainingSizeBytes <= maxSizeBytes;

            return result;
        }
    }

    public class CleanupResult
    {
        public int FilesDeleted { get; set; }
        public long BytesFreed { get; set; }
        public long RemainingSizeBytes { get; set; }
        public bool SizeTargetAchieved { get; set; }
    }
}
