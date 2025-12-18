using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CallManagement.ViewModels;

/// <summary>
/// ViewModel for the Daily Report Preview popup.
/// Shows markdown preview before sending via Telegram.
/// </summary>
public partial class DailyReportPreviewViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The markdown content to preview (same as what will be sent)
    /// </summary>
    [ObservableProperty]
    private string _reportMarkdown = string.Empty;

    /// <summary>
    /// The date for the report
    /// </summary>
    [ObservableProperty]
    private DateTime _reportDate = DateTime.Today;

    /// <summary>
    /// Whether the report is currently being sent
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendReportCommand))]
    private bool _isSending;

    /// <summary>
    /// Whether there is data to show
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendReportCommand))]
    private bool _hasData;

    /// <summary>
    /// Error message if any
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════════════════

    private readonly DatabaseService _databaseService;
    private CancellationTokenSource? _sendCts;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when the popup should be closed
    /// </summary>
    public event Action? CloseRequested;

    /// <summary>
    /// Raised when report was sent successfully
    /// </summary>
    public event Action<DailyReportResult>? ReportSent;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public DailyReportPreviewViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialize the preview by loading report content for the specified date.
    /// </summary>
    public async Task LoadPreviewAsync(DateTime date)
    {
        ReportDate = date;
        ErrorMessage = null;
        HasData = false;

        try
        {
            using var reportService = new DailyReportService(_databaseService);
            
            // Check configuration first
            if (!await reportService.IsConfiguredAsync())
            {
                ErrorMessage = "Telegram chưa được cấu hình. Vui lòng vào Settings để cấu hình.";
                ReportMarkdown = "_Telegram chưa được cấu hình._";
                return;
            }

            // Get preview markdown
            var markdown = await reportService.GetReportPreviewAsync(date);

            if (string.IsNullOrWhiteSpace(markdown) || markdown.Contains("Tổng số cuộc gọi: *0*"))
            {
                ReportMarkdown = $"_Không có dữ liệu cuộc gọi cho ngày {date:dd/MM/yyyy}._";
                HasData = false;
            }
            else
            {
                ReportMarkdown = markdown;
                HasData = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải preview: {ex.Message}";
            ReportMarkdown = "_Lỗi khi tải dữ liệu._";
            System.Diagnostics.Debug.WriteLine($"LoadPreviewAsync error: {ex}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Send the report via Telegram
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendReport))]
    private async Task SendReport()
    {
        if (IsSending) return;

        IsSending = true;
        ErrorMessage = null;
        _sendCts = new CancellationTokenSource();

        try
        {
            using var reportService = new DailyReportService(_databaseService);
            var result = await reportService.SendDailyReportAsync(ReportDate, _sendCts.Token);

            if (result.IsSuccess)
            {
                ReportSent?.Invoke(result);
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Gửi report thất bại";
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled, do nothing
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SendReport error: {ex}");
        }
        finally
        {
            IsSending = false;
            _sendCts?.Dispose();
            _sendCts = null;
        }
    }

    private bool CanSendReport() => HasData && !IsSending;

    /// <summary>
    /// Cancel and close the popup
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _sendCts?.Cancel();
        CloseRequested?.Invoke();
    }

    /// <summary>
    /// Close the popup (same as Cancel)
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        Cancel();
    }
}
