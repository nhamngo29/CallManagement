using System.Collections.Generic;

namespace CallManagement.Models
{
    /// <summary>
    /// Result of an Excel import operation.
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// Successfully imported contacts.
        /// </summary>
        public IReadOnlyList<Contact> Contacts { get; init; } = new List<Contact>();

        /// <summary>
        /// Number of rows successfully imported.
        /// </summary>
        public int SuccessCount => Contacts.Count;

        /// <summary>
        /// Number of rows skipped due to validation errors.
        /// </summary>
        public int SkippedCount { get; init; }

        /// <summary>
        /// Total rows processed (including header).
        /// </summary>
        public int TotalRows { get; init; }

        /// <summary>
        /// Detailed skip reasons for reporting.
        /// Key: reason, Value: count
        /// </summary>
        public IReadOnlyDictionary<string, int> SkipReasons { get; init; } = new Dictionary<string, int>();

        /// <summary>
        /// Whether the import was successful (at least 1 contact imported).
        /// </summary>
        public bool IsSuccess => SuccessCount > 0;

        /// <summary>
        /// Error message if import failed completely.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Get a summary message for display.
        /// </summary>
        public string GetSummaryMessage()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                return ErrorMessage;
            }

            var parts = new List<string>();
            parts.Add($"Đã import {SuccessCount} liên hệ");

            if (SkippedCount > 0)
            {
                var skipDetails = new List<string>();
                foreach (var reason in SkipReasons)
                {
                    skipDetails.Add($"{reason.Value} {reason.Key}");
                }
                parts.Add($"Bỏ qua {SkippedCount} dòng ({string.Join(", ", skipDetails)})");
            }

            return string.Join(". ", parts);
        }
    }
}
