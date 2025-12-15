using System;

namespace CallManagement.Models
{
    /// <summary>
    /// Represents a saved call session snapshot.
    /// Each session is created when user clicks "Save Session".
    /// </summary>
    public class CallSession
    {
        /// <summary>
        /// Primary key, auto-increment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique session key in format: ddMMyyHHmm
        /// Example: "1512251430" = 15/12/25 14:30
        /// </summary>
        public string SessionKey { get; set; } = string.Empty;

        /// <summary>
        /// ISO datetime string when session was created.
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Optional display name for the session.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Total contacts count in this session.
        /// </summary>
        public int ContactCount { get; set; }

        /// <summary>
        /// Get formatted display text for tab header.
        /// </summary>
        public string FormattedDate
        {
            get
            {
                if (DateTime.TryParse(CreatedAt, out var date))
                {
                    return date.ToString("dd/MM/yy HH:mm");
                }
                return SessionKey;
            }
        }
    }
}
