using System;

namespace CallManagement.Services
{
    /// <summary>
    /// Service interface for scheduling automatic daily report sends.
    /// </summary>
    public interface IDailyReportScheduler
    {
        /// <summary>
        /// Start the scheduler to watch for configured send time.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the scheduler.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets whether the scheduler is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Event raised when an auto-send is triggered.
        /// </summary>
        event EventHandler<DailyReportSentEventArgs>? DailyReportSent;
    }

    /// <summary>
    /// Event args for daily report sent event.
    /// </summary>
    public class DailyReportSentEventArgs : EventArgs
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public DateTime SentAt { get; set; }
    }
}
