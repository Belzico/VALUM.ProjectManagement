namespace VALUM.Api.Logging
{
    public class LogCleanupOptions
    {
        private bool _enabled = false;
        private int _intervalMinutes = 60;
        private int _maxAgeDays = 7;
        private int _maxTotalSizeMB = 100;
        private bool _preventDeletion = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public int IntervalMinutes
        {
            get => _intervalMinutes;
            set => _intervalMinutes = value > 0 ? value : 60;
        }

        public int MaxAgeDays
        {
            get => _maxAgeDays;
            set => _maxAgeDays = value > 0 ? value : 7;
        }

        public int MaxTotalSizeMB
        {
            get => _maxTotalSizeMB;
            set => _maxTotalSizeMB = value > 0 ? value : 100;
        }

        public bool PreventDeletion
        {
            get => _preventDeletion;
            set => _preventDeletion = value;
        }
    }
}
