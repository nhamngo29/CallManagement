namespace CallManagement.Models;

/// <summary>
/// Defines the types of toast notifications available in the application.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Success notification - green color, checkmark icon
    /// Auto-hide after 2-3 seconds
    /// </summary>
    Success,
    
    /// <summary>
    /// Informational notification - blue color, info icon
    /// Auto-hide after 2-3 seconds
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning notification - orange color, warning icon
    /// Auto-hide after 4-5 seconds
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error notification - red color, error icon
    /// Auto-hide after 4-5 seconds
    /// </summary>
    Error
}
