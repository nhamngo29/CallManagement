using CallManagement.Models;
using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CallManagement.ViewModels;

/// <summary>
/// ViewModel for the notification toast overlay.
/// Binds to the NotificationService singleton to display notifications.
/// </summary>
public partial class NotificationViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Gets the notification service singleton
    /// </summary>
    public NotificationService NotificationService => NotificationService.Instance;
    
    /// <summary>
    /// Gets the current notification to display
    /// </summary>
    public NotificationItem? CurrentNotification => NotificationService.CurrentNotification;
    
    /// <summary>
    /// Gets whether a notification is currently visible
    /// </summary>
    public bool IsNotificationVisible => NotificationService.IsNotificationVisible;
    
    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════
    
    public NotificationViewModel()
    {
        // Subscribe to service property changes
        NotificationService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NotificationService.CurrentNotification))
            {
                OnPropertyChanged(nameof(CurrentNotification));
            }
            else if (e.PropertyName == nameof(NotificationService.IsNotificationVisible))
            {
                OnPropertyChanged(nameof(IsNotificationVisible));
            }
        };
    }
}
