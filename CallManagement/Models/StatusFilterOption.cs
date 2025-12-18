namespace CallManagement.Models
{
    /// <summary>
    /// Represents a status filter option for the contact list dropdown.
    /// </summary>
    public class StatusFilterOption
    {
        /// <summary>
        /// Display text for the filter option.
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// The CallStatus value to filter by. Null means "All" (no filter).
        /// </summary>
        public CallStatus? StatusValue { get; }

        /// <summary>
        /// Icon/emoji for the filter option.
        /// </summary>
        public string Icon { get; }

        public StatusFilterOption(string displayText, CallStatus? statusValue, string icon = "")
        {
            DisplayText = displayText;
            StatusValue = statusValue;
            Icon = icon;
        }

        public override string ToString() => string.IsNullOrEmpty(Icon) 
            ? DisplayText 
            : $"{Icon} {DisplayText}";

        /// <summary>
        /// Predefined filter options.
        /// </summary>
        public static StatusFilterOption[] AllOptions { get; } = new[]
        {
            new StatusFilterOption("Tất cả", null, "📋"),
            new StatusFilterOption("Chưa gọi", CallStatus.None, "⏸️"),
            new StatusFilterOption("Có nhu cầu", CallStatus.Interested, "👍"),
            new StatusFilterOption("Không nhu cầu", CallStatus.NotInterested, "👎"),
            new StatusFilterOption("Không bắt máy", CallStatus.NoAnswer, "🔕"),
            new StatusFilterOption("Máy bận", CallStatus.Busy, "⏳"),
            new StatusFilterOption("Số không tồn tại", CallStatus.InvalidNumber, "🚫")
        };
    }
}
