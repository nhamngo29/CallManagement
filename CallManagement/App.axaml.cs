using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CallManagement.Services;
using CallManagement.ViewModels;
using CallManagement.Views;
using System;
using System.Linq;

namespace CallManagement
{
    public partial class App : Application
    {
        private DailyReportScheduler? _dailyReportScheduler;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                desktop.MainWindow = mainWindow;

                // Initialize and start Daily Report Scheduler
                desktop.ShutdownRequested += OnShutdownRequested;
                
                // Start scheduler after main window is shown
                mainWindow.Opened += async (_, _) =>
                {
                    await InitializeDailyReportSchedulerAsync();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Initialize and start the daily report scheduler.
        /// </summary>
        private async System.Threading.Tasks.Task InitializeDailyReportSchedulerAsync()
        {
            try
            {
                // Wait for database service to be available
                if (DatabaseService.Instance == null)
                {
                    System.Diagnostics.Debug.WriteLine("[App] DatabaseService not yet initialized");
                    return;
                }

                _dailyReportScheduler = new DailyReportScheduler(DatabaseService.Instance);
                _dailyReportScheduler.DailyReportSent += OnDailyReportSent;
                _dailyReportScheduler.Start();

                System.Diagnostics.Debug.WriteLine("[App] Daily Report Scheduler started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Failed to start scheduler: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle daily report sent event - show notification.
        /// </summary>
        private void OnDailyReportSent(object? sender, DailyReportSentEventArgs e)
        {
            var notificationService = NotificationService.Instance;
            if (notificationService == null) return;

            if (e.IsSuccess)
            {
                notificationService.ShowSuccess(e.Message ?? "Daily Report đã gửi thành công");
            }
            else
            {
                notificationService.ShowError($"Daily Report gửi thất bại: {e.Message}");
            }
        }

        /// <summary>
        /// Clean up scheduler on app shutdown.
        /// </summary>
        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            _dailyReportScheduler?.Stop();
            _dailyReportScheduler?.Dispose();
            _dailyReportScheduler = null;
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}