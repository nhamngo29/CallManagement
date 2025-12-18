using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace CallManagement.Services
{
    /// <summary>
    /// Service for scheduling automatic daily report sends.
    /// Runs in background and checks every minute if it's time to send.
    /// </summary>
    public class DailyReportScheduler : IDailyReportScheduler, IDisposable
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromMinutes(1);

        // ═══════════════════════════════════════════════════════════════════════
        // FIELDS
        // ═══════════════════════════════════════════════════════════════════════

        private readonly DatabaseService _databaseService;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _schedulerTask;
        private bool _disposed;

        /// <summary>
        /// Singleton instance for app-wide access.
        /// </summary>
        public static DailyReportScheduler? Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        /// <inheritdoc/>
        public event EventHandler<DailyReportSentEventArgs>? DailyReportSent;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════

        public DailyReportScheduler(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            Instance = this;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public void Start()
        {
            if (IsRunning) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _schedulerTask = RunSchedulerLoopAsync(_cancellationTokenSource.Token);
            IsRunning = true;

            System.Diagnostics.Debug.WriteLine("[DailyReportScheduler] Started");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!IsRunning) return;

            _cancellationTokenSource?.Cancel();
            IsRunning = false;

            System.Diagnostics.Debug.WriteLine("[DailyReportScheduler] Stopped");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Main scheduler loop - checks every minute if it's time to send.
        /// </summary>
        private async Task RunSchedulerLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendReportAsync(cancellationToken);
                    await Task.Delay(CHECK_INTERVAL, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DailyReportScheduler] Error in loop: {ex.Message}");
                    // Continue running despite errors
                    await Task.Delay(CHECK_INTERVAL, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Check if it's time to send and send if conditions are met.
        /// </summary>
        private async Task CheckAndSendReportAsync(CancellationToken cancellationToken)
        {
            if (SettingsService.Instance == null) return;

            // Load auto-send settings
            var autoSendSettings = await SettingsService.Instance.LoadDailyReportSettingsAsync();

            // Check if auto-send is enabled
            if (!autoSendSettings.IsEnabled)
            {
                return;
            }

            var now = DateTime.Now;
            var configuredTime = autoSendSettings.SendTime;

            // Check if current time matches configured time (within 1 minute)
            if (now.Hour != configuredTime.Hours || now.Minute != configuredTime.Minutes)
            {
                return;
            }

            // Check if already sent today
            if (autoSendSettings.LastSentDate.HasValue &&
                autoSendSettings.LastSentDate.Value.Date == now.Date)
            {
                return; // Already sent today
            }

            // Time to send!
            System.Diagnostics.Debug.WriteLine($"[DailyReportScheduler] Triggering auto-send at {now:HH:mm}");

            try
            {
                using var reportService = new DailyReportService(_databaseService);
                var result = await reportService.SendDailyReportAsync(now.Date, cancellationToken);

                if (result.IsSuccess)
                {
                    // Update last sent date
                    await SettingsService.Instance.SaveLastDailyReportSentDateAsync(now);

                    // Raise event on UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        DailyReportSent?.Invoke(this, new DailyReportSentEventArgs
                        {
                            IsSuccess = true,
                            Message = $"Daily report gửi thành công ({result.TotalCalls} cuộc gọi)",
                            SentAt = now
                        });
                    });

                    System.Diagnostics.Debug.WriteLine($"[DailyReportScheduler] Auto-send successful: {result.TotalCalls} calls");
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        DailyReportSent?.Invoke(this, new DailyReportSentEventArgs
                        {
                            IsSuccess = false,
                            Message = result.ErrorMessage,
                            SentAt = now
                        });
                    });

                    System.Diagnostics.Debug.WriteLine($"[DailyReportScheduler] Auto-send failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyReportScheduler] Auto-send exception: {ex.Message}");

                Dispatcher.UIThread.Post(() =>
                {
                    DailyReportSent?.Invoke(this, new DailyReportSentEventArgs
                    {
                        IsSuccess = false,
                        Message = $"Lỗi: {ex.Message}",
                        SentAt = now
                    });
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _cancellationTokenSource?.Dispose();
            _disposed = true;

            if (Instance == this)
            {
                Instance = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
