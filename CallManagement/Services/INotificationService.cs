namespace CallManagement.Services;

/// <summary>
/// Service interface for displaying toast notifications throughout the application.
/// ViewModels should inject this service and call its methods to show notifications.
/// Only ONE notification is displayed at a time - new notifications immediately dismiss any existing one.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a success notification (green, checkmark icon)
    /// Auto-hides after 2-3 seconds
    /// Immediately dismisses any existing notification
    /// </summary>
    /// <param name="message">The message to display</param>
    void ShowSuccess(string message);
    
    /// <summary>
    /// Shows an informational notification (blue, info icon)
    /// Auto-hides after 2-3 seconds
    /// Immediately dismisses any existing notification
    /// </summary>
    /// <param name="message">The message to display</param>
    void ShowInfo(string message);
    
    /// <summary>
    /// Shows a warning notification (orange, warning icon)
    /// Auto-hides after 4-5 seconds
    /// Immediately dismisses any existing notification
    /// </summary>
    /// <param name="message">The message to display</param>
    void ShowWarning(string message);
    
    /// <summary>
    /// Shows an error notification (red, error icon)
    /// Auto-hides after 4-5 seconds
    /// Immediately dismisses any existing notification
    /// </summary>
    /// <param name="message">The message to display</param>
    void ShowError(string message);
    
    /// <summary>
    /// Immediately dismisses the current notification if one is showing.
    /// This is called automatically by ShowXxx methods.
    /// </summary>
    void DismissCurrent();
}
