using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CallManagement.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CallManagement.Services;

/// <summary>
/// Implementation of notification service with IMMEDIATE REPLACE behavior.
/// Only ONE notification is displayed at a time - new notifications immediately dismiss any existing one.
/// NO queuing, NO stacking - just instant replacement.
/// </summary>
public partial class NotificationService : ObservableObject, INotificationService
{
    // ═══════════════════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════════════════
    
    private const int ShortDuration = 2500;  // 2.5 seconds for success/info
    private const int LongDuration = 4000;   // 4 seconds for warning/error
    private const int FadeOutDuration = 80;  // Fast fade out for old toast
    
    // ═══════════════════════════════════════════════════════════════════════
    // SINGLETON INSTANCE
    // ═══════════════════════════════════════════════════════════════════════
    
    private static NotificationService? _instance;
    private static readonly object _lock = new();
    
    /// <summary>
    /// Gets the singleton instance of the notification service.
    /// </summary>
    public static NotificationService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new NotificationService();
                }
            }
            return _instance;
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// The currently displayed notification (null if none)
    /// </summary>
    [ObservableProperty]
    private NotificationItem? _currentNotification;
    
    /// <summary>
    /// Whether a notification is currently being displayed
    /// </summary>
    [ObservableProperty]
    private bool _isNotificationVisible;
    
    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════════════════
    
    private CancellationTokenSource? _autoHideCts;
    private readonly object _showLock = new();
    
    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════
    
    private NotificationService()
    {
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC METHODS (INotificationService)
    // ═══════════════════════════════════════════════════════════════════════
    
    public void ShowSuccess(string message)
    {
        ShowNotification(new NotificationItem
        {
            Message = message,
            Type = NotificationType.Success,
            Duration = ShortDuration
        });
    }
    
    public void ShowInfo(string message)
    {
        ShowNotification(new NotificationItem
        {
            Message = message,
            Type = NotificationType.Info,
            Duration = ShortDuration
        });
    }
    
    public void ShowWarning(string message)
    {
        ShowNotification(new NotificationItem
        {
            Message = message,
            Type = NotificationType.Warning,
            Duration = LongDuration
        });
    }
    
    public void ShowError(string message)
    {
        ShowNotification(new NotificationItem
        {
            Message = message,
            Type = NotificationType.Error,
            Duration = LongDuration
        });
    }
    
    public void DismissCurrent()
    {
        Dispatcher.UIThread.Post(() =>
        {
            DismissCurrentInternal();
        });
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════════════════
    
    private void ShowNotification(NotificationItem notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            lock (_showLock)
            {
                // Step 1: Cancel any pending auto-hide
                _autoHideCts?.Cancel();
                _autoHideCts?.Dispose();
                _autoHideCts = null;
                
                // Step 2: Immediately dismiss current (no animation delay)
                DismissCurrentInternal();
                
                // Step 3: Show new notification immediately
                CurrentNotification = notification;
                notification.IsVisible = true;
                IsNotificationVisible = true;
                
                // Step 4: Schedule auto-hide
                _autoHideCts = new CancellationTokenSource();
                ScheduleAutoHide(notification.Duration, _autoHideCts.Token);
            }
        });
    }
    
    private void DismissCurrentInternal()
    {
        // Immediate dismiss - no fade animation when replacing
        if (CurrentNotification != null)
        {
            CurrentNotification.IsVisible = false;
        }
        IsNotificationVisible = false;
        CurrentNotification = null;
    }
    
    private async void ScheduleAutoHide(int duration, CancellationToken token)
    {
        try
        {
            // Wait for display duration
            await Task.Delay(duration, token);
            
            if (token.IsCancellationRequested) return;
            
            // Start fade out animation
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (CurrentNotification != null)
                {
                    IsNotificationVisible = false;
                }
            });
            
            // Wait for fade animation
            await Task.Delay(FadeOutDuration, token);
            
            if (token.IsCancellationRequested) return;
            
            // Clear the notification
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!token.IsCancellationRequested)
                {
                    if (CurrentNotification != null)
                    {
                        CurrentNotification.IsVisible = false;
                    }
                    CurrentNotification = null;
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Expected when a new notification replaces this one
        }
        catch (ObjectDisposedException)
        {
            // CTS was disposed, ignore
        }
    }
}
