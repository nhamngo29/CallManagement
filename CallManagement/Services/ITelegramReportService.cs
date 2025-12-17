using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Result of sending a single report file.
    /// </summary>
    public class SendReportResult
    {
        public string SessionKey { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Progress callback for report sending.
    /// </summary>
    public class SendReportProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string CurrentSessionKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service interface for sending Excel reports via Telegram Bot API.
    /// </summary>
    public interface ITelegramReportService
    {
        /// <summary>
        /// Check if Telegram is properly configured.
        /// </summary>
        Task<bool> IsConfiguredAsync();

        /// <summary>
        /// Validate Telegram configuration by testing the connection.
        /// </summary>
        /// <returns>Error message if invalid, null if valid.</returns>
        Task<string?> ValidateConfigurationAsync();

        /// <summary>
        /// Send Excel reports for multiple sessions via Telegram.
        /// </summary>
        /// <param name="sessionKeys">Session keys to export and send.</param>
        /// <param name="progressCallback">Optional progress callback.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of results for each session.</returns>
        Task<List<SendReportResult>> SendExcelReportsAsync(
            IEnumerable<string> sessionKeys,
            System.Action<SendReportProgress>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
