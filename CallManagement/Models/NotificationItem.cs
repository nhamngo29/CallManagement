using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CallManagement.Models;

/// <summary>
/// Represents a single notification item in the toast queue.
/// </summary>
public partial class NotificationItem : ObservableObject
{
    /// <summary>
    /// Unique identifier for this notification
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
    
    /// <summary>
    /// The message to display
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// The type of notification (Success, Info, Warning, Error)
    /// </summary>
    public NotificationType Type { get; init; }
    
    /// <summary>
    /// Duration in milliseconds before auto-hide
    /// </summary>
    public int Duration { get; init; }
    
    /// <summary>
    /// Timestamp when the notification was created
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;
    
    /// <summary>
    /// Whether the notification is currently visible (for animation)
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;
    
    /// <summary>
    /// Icon text based on notification type
    /// </summary>
    public string Icon => Type switch
    {
        NotificationType.Success => "✓",
        NotificationType.Info => "ℹ",
        NotificationType.Warning => "⚠",
        NotificationType.Error => "✕",
        _ => "ℹ"
    };
}
