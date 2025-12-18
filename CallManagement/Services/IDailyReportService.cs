using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Service interface for generating and sending daily call reports via Telegram.
    /// </summary>
    public interface IDailyReportService
    {
        /// <summary>
        /// Send daily report for the specified date via Telegram.
        /// </summary>
        /// <param name="date">The date to generate the report for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if sent successfully, false otherwise.</returns>
        Task<DailyReportResult> SendDailyReportAsync(DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if Telegram is properly configured for sending reports.
        /// </summary>
        Task<bool> IsConfiguredAsync();

        /// <summary>
        /// Get a preview of the daily report markdown content.
        /// </summary>
        /// <param name="date">The date to generate the report for.</param>
        Task<string> GetReportPreviewAsync(DateTime date);
    }

    /// <summary>
    /// Result of sending a daily report.
    /// </summary>
    public class DailyReportResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalCalls { get; set; }
        public int InterestedCount { get; set; }
        public int NotInterestedCount { get; set; }
    }
}
