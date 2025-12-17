using CallManagement.Models;
using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CallManagement.ViewModels
{
    /// <summary>
    /// ViewModel for the Send Report popup.
    /// Handles session filtering, selection, and sending reports via Telegram.
    /// </summary>
    public partial class SendReportViewModel : ViewModelBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // FILTER OPTIONS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _filterToday = true;

        [ObservableProperty]
        private bool _filterDateRange;

        [ObservableProperty]
        private DateTimeOffset _filterFromDate = DateTimeOffset.Now.Date;

        [ObservableProperty]
        private DateTimeOffset _filterToDate = DateTimeOffset.Now.Date;

        // ═══════════════════════════════════════════════════════════════════════
        // SESSIONS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private ObservableCollection<SelectableSessionViewModel> _sessions = new();

        [ObservableProperty]
        private ObservableCollection<SelectableSessionViewModel> _filteredSessions = new();

        [ObservableProperty]
        private bool _selectAll;

        public int SelectedCount => FilteredSessions.Count(s => s.IsSelected);

        public bool HasSelectedSessions => SelectedCount > 0;

        // ═══════════════════════════════════════════════════════════════════════
        // SENDING STATE
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _isSending;

        [ObservableProperty]
        private string _sendingProgress = string.Empty;

        [ObservableProperty]
        private int _progressCurrent;

        [ObservableProperty]
        private int _progressTotal;

        // ═══════════════════════════════════════════════════════════════════════
        // RESULT & STATUS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _showResult;

        [ObservableProperty]
        private bool _isResultSuccess;

        [ObservableProperty]
        private string _resultMessage = string.Empty;

        [ObservableProperty]
        private string _errorDetails = string.Empty;

        // ═══════════════════════════════════════════════════════════════════════
        // SERVICES
        // ═══════════════════════════════════════════════════════════════════════

        private DatabaseService? _databaseService;
        private ITelegramReportService? _telegramService;
        private CancellationTokenSource? _cancellationTokenSource;

        // ═══════════════════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════════════════

        public event Action? RequestClose;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR & INITIALIZATION
        // ═══════════════════════════════════════════════════════════════════════

        public SendReportViewModel()
        {
        }

        /// <summary>
        /// Initialize the ViewModel with required services.
        /// </summary>
        public async Task InitializeAsync(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _telegramService = new TelegramReportService(databaseService);

            await LoadSessionsAsync();
            ApplyFilter();
        }

        /// <summary>
        /// Load all sessions from database.
        /// </summary>
        private async Task LoadSessionsAsync()
        {
            if (_databaseService == null) return;

            var dbSessions = await _databaseService.GetAllSessionsAsync();
            Sessions.Clear();

            foreach (var session in dbSessions.OrderByDescending(s => s.CreatedAt))
            {
                var vm = new SelectableSessionViewModel(
                    session.SessionKey,
                    session.FormattedDate,
                    session.ContactCount,
                    session.CreatedAt
                );

                // Subscribe to selection changes
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectableSessionViewModel.IsSelected))
                    {
                        OnPropertyChanged(nameof(SelectedCount));
                        OnPropertyChanged(nameof(HasSelectedSessions));
                        UpdateSelectAllState();
                    }
                };

                Sessions.Add(vm);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // FILTER LOGIC
        // ═══════════════════════════════════════════════════════════════════════

        partial void OnFilterTodayChanged(bool value)
        {
            if (value)
            {
                FilterDateRange = false;
            }
            ApplyFilter();
        }

        partial void OnFilterDateRangeChanged(bool value)
        {
            if (value)
            {
                FilterToday = false;
            }
            ApplyFilter();
        }

        partial void OnFilterFromDateChanged(DateTimeOffset value)
        {
            if (FilterDateRange)
            {
                ApplyFilter();
            }
        }

        partial void OnFilterToDateChanged(DateTimeOffset value)
        {
            if (FilterDateRange)
            {
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            FilteredSessions.Clear();

            foreach (var session in Sessions)
            {
                bool include = false;

                if (DateTime.TryParse(session.CreatedAt, out var createdDate))
                {
                    if (FilterToday)
                    {
                        include = createdDate.Date == DateTime.Today;
                    }
                    else if (FilterDateRange)
                    {
                        include = createdDate.Date >= FilterFromDate.Date &&
                                  createdDate.Date <= FilterToDate.Date;
                    }
                    else
                    {
                        // No filter, show all
                        include = true;
                    }
                }

                if (include)
                {
                    FilteredSessions.Add(session);
                }
            }

            // Reset selection states
            SelectAll = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedSessions));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SELECT ALL
        // ═══════════════════════════════════════════════════════════════════════

        partial void OnSelectAllChanged(bool value)
        {
            foreach (var session in FilteredSessions)
            {
                session.IsSelected = value;
            }
        }

        private void UpdateSelectAllState()
        {
            if (FilteredSessions.Count == 0)
            {
                // Don't change SelectAll
                return;
            }

            bool allSelected = FilteredSessions.All(s => s.IsSelected);
            bool noneSelected = FilteredSessions.All(s => !s.IsSelected);

            if (allSelected && !SelectAll)
            {
                SelectAll = true;
            }
            else if (noneSelected && SelectAll)
            {
                SelectAll = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SEND COMMAND
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task SendAsync()
        {
            if (_telegramService == null || !HasSelectedSessions)
                return;

            // Validate Telegram configuration
            var validationError = await _telegramService.ValidateConfigurationAsync();
            if (validationError != null)
            {
                ShowError("Lỗi cấu hình Telegram", validationError);
                return;
            }

            IsSending = true;
            ShowResult = false;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var selectedKeys = FilteredSessions
                    .Where(s => s.IsSelected)
                    .Select(s => s.SessionKey)
                    .ToList();

                ProgressTotal = selectedKeys.Count;
                ProgressCurrent = 0;

                var results = await _telegramService.SendExcelReportsAsync(
                    selectedKeys,
                    progress =>
                    {
                        ProgressCurrent = progress.Current;
                        SendingProgress = $"Đang gửi {progress.Current}/{progress.Total}...";
                    },
                    _cancellationTokenSource.Token);

                // Process results
                var successCount = results.Count(r => r.IsSuccess);
                var failedResults = results.Where(r => !r.IsSuccess).ToList();

                if (failedResults.Count == 0)
                {
                    ShowSuccess($"Đã gửi thành công {successCount} file!");
                }
                else if (successCount > 0)
                {
                    var errors = string.Join("\n", failedResults.Select(r => $"• {r.SessionKey}: {r.ErrorMessage}"));
                    ShowError(
                        $"Gửi thành công {successCount}/{results.Count} file",
                        $"Các file lỗi:\n{errors}");
                }
                else
                {
                    var errors = string.Join("\n", failedResults.Select(r => $"• {r.SessionKey}: {r.ErrorMessage}"));
                    ShowError("Gửi thất bại", errors);
                }
            }
            catch (OperationCanceledException)
            {
                ShowError("Đã hủy", "Quá trình gửi đã bị hủy");
            }
            catch (Exception ex)
            {
                ShowError("Lỗi", ex.Message);
            }
            finally
            {
                IsSending = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CANCEL COMMAND
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void Cancel()
        {
            if (IsSending)
            {
                _cancellationTokenSource?.Cancel();
            }
            else
            {
                RequestClose?.Invoke();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CLOSE COMMAND
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void Close()
        {
            RequestClose?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RESULT HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        private void ShowSuccess(string message)
        {
            ResultMessage = message;
            ErrorDetails = string.Empty;
            IsResultSuccess = true;
            ShowResult = true;
        }

        private void ShowError(string message, string details = "")
        {
            ResultMessage = message;
            ErrorDetails = details;
            IsResultSuccess = false;
            ShowResult = true;
        }

        [RelayCommand]
        private void DismissResult()
        {
            ShowResult = false;
            
            // Auto close if success
            if (IsResultSuccess)
            {
                RequestClose?.Invoke();
            }
        }
    }
}
