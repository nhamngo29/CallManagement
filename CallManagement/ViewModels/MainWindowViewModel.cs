using Avalonia;
using CallManagement.Models;
using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CallManagement.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // SIDEBAR SESSION LIST
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private ObservableCollection<SessionListItemViewModel> _sessions = new();

        [ObservableProperty]
        private SessionListItemViewModel? _selectedSession;

        [ObservableProperty]
        private bool _isSidebarVisible = true;

        // Flag to prevent infinite loop when syncing sidebar and tab selection
        private bool _isSyncingSelection;

        // ═══════════════════════════════════════════════════════════════════════
        // TABS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private ObservableCollection<SessionTabViewModel> _tabs = new();

        [ObservableProperty]
        private SessionTabViewModel? _selectedTab;

        public SessionTabViewModel? CurrentSessionTab => Tabs.FirstOrDefault(t => t.IsCurrentSession);

        public ObservableCollection<Contact> Contacts => SelectedTab?.Contacts ?? new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isFirstLaunch = true;

        [ObservableProperty]
        private bool _showOnboarding = false;

        [ObservableProperty]
        private int _currentOnboardingStep = 1;

        public int TotalOnboardingSteps => 3;

        [ObservableProperty]
        private bool _showHelpTooltips = false;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _showDeleteConfirmation;

        [ObservableProperty]
        private SessionTabViewModel? _sessionToDelete;

        // ═══════════════════════════════════════════════════════════════════════
        // SETTINGS VIEW
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _showSettings;

        [ObservableProperty]
        private SettingsViewModel? _settingsViewModel;

        // ═══════════════════════════════════════════════════════════════════════
        // SEND REPORT VIEW
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _showSendReport;

        [ObservableProperty]
        private SendReportViewModel? _sendReportViewModel;

        [ObservableProperty]
        private bool _isTelegramConfigured;

        /// <summary>
        /// Whether auto-send daily report is enabled.
        /// When true, the manual Send Daily Report button should be hidden.
        /// </summary>
        [ObservableProperty]
        private bool _isAutoSendDailyReportEnabled;

        // ═══════════════════════════════════════════════════════════════════════
        // DAILY REPORT PREVIEW
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _showDailyReportPreview;

        [ObservableProperty]
        private DailyReportPreviewViewModel? _dailyReportPreviewViewModel;

        // ═══════════════════════════════════════════════════════════════════════
        // NOTIFICATION SERVICE
        // ═══════════════════════════════════════════════════════════════════════

        public NotificationViewModel NotificationViewModel { get; } = new();
        
        private INotificationService NotificationService => Services.NotificationService.Instance;

        public int TotalCount => SelectedTab?.TotalCount ?? 0;
        public int InterestedCount => SelectedTab?.InterestedCount ?? 0;
        public int NotInterestedCount => SelectedTab?.NotInterestedCount ?? 0;
        public int NoAnswerCount => SelectedTab?.NoAnswerCount ?? 0;
        public int BusyCount => SelectedTab?.BusyCount ?? 0;
        public int InvalidCount => SelectedTab?.InvalidCount ?? 0;

        public bool IsCurrentTabEditable => SelectedTab?.IsCurrentSession ?? false;

        private DatabaseService? _databaseService;
        private readonly IExcelService _excelService = new ExcelService();

        // Reference to MainWindow for file dialogs
        public Avalonia.Controls.Window? MainWindow { get; set; }

        public MainWindowViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            StatusMessage = "Dang khoi tao...";

            try
            {
                _databaseService = new DatabaseService();
                await _databaseService.InitializeAsync();

                // Initialize settings service for Telegram check
                var settingsService = new SettingsService();
                await settingsService.InitializeAsync();

                // Create current session tab
                var currentTab = new SessionTabViewModel();
                Tabs.Add(currentTab);

                // Create current session sidebar item
                var currentSessionItem = new SessionListItemViewModel { Tab = currentTab };
                Sessions.Add(currentSessionItem);

                // Load history sessions
                await LoadHistorySessionsAsync();

                // Select current session
                SelectedTab = currentTab;
                SelectedSession = currentSessionItem;

                LoadSampleData();

                CheckFirstLaunch();

                // Check Telegram configuration
                await CheckTelegramConfigurationAsync();

                // Load auto-send daily report setting
                await LoadAutoSendDailyReportSettingAsync();

                // Subscribe to settings changes
                if (SettingsService.Instance != null)
                {
                    SettingsService.Instance.DailyReportSettingsChanged += OnDailyReportSettingsChanged;
                }

                // Subscribe to DailyReportScheduler events for toast notifications
                if (DailyReportScheduler.Instance != null)
                {
                    DailyReportScheduler.Instance.DailyReportSent += OnDailyReportAutoSent;
                }

                StatusMessage = "San sang";
            }
            catch (Exception ex)
            {
                StatusMessage = "Loi: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Initialize failed: " + ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadHistorySessionsAsync()
        {
            if (_databaseService == null) return;

            var sessions = await _databaseService.GetAllSessionsAsync();
            foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
            {
                // Create tab
                var tab = new SessionTabViewModel(session);
                Tabs.Add(tab);

                // Create sidebar item linked to tab
                var sidebarItem = new SessionListItemViewModel(session) { Tab = tab };
                Sessions.Add(sidebarItem);
            }
        }

        partial void OnSelectedTabChanged(SessionTabViewModel? value)
        {
            if (value == null) return;

            if (!value.IsLoaded && !value.IsCurrentSession)
            {
                _ = value.LoadContactsAsync();
            }

            // Sync sidebar selection with tab
            SyncSidebarSelection(value);

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(InterestedCount));
            OnPropertyChanged(nameof(NotInterestedCount));
            OnPropertyChanged(nameof(NoAnswerCount));
            OnPropertyChanged(nameof(BusyCount));
            OnPropertyChanged(nameof(InvalidCount));
            OnPropertyChanged(nameof(IsCurrentTabEditable));
            OnPropertyChanged(nameof(Contacts));
        }

        /// <summary>
        /// Handle sidebar selection change - load corresponding session.
        /// ListBox handles visual selection, we just need to sync Tab.
        /// </summary>
        partial void OnSelectedSessionChanged(SessionListItemViewModel? value)
        {
            if (value == null || _isSyncingSelection) return;

            // Find or create corresponding tab
            var tab = value.Tab ?? FindOrCreateTabForSession(value);
            if (tab != null)
            {
                _isSyncingSelection = true;
                try
                {
                    // Always set the tab (even if same, to trigger load)
                    SelectedTab = tab;
                    
                    // Ensure data is loaded for history sessions
                    if (!tab.IsLoaded && !tab.IsCurrentSession)
                    {
                        _ = LoadSessionDataAsync(tab);
                    }
                }
                finally
                {
                    _isSyncingSelection = false;
                }
            }
        }

        /// <summary>
        /// Load session data and update UI.
        /// </summary>
        private async Task LoadSessionDataAsync(SessionTabViewModel tab)
        {
            await tab.LoadContactsAsync();
            
            // Update UI after data loaded
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(InterestedCount));
            OnPropertyChanged(nameof(NotInterestedCount));
            OnPropertyChanged(nameof(NoAnswerCount));
            OnPropertyChanged(nameof(BusyCount));
            OnPropertyChanged(nameof(InvalidCount));
            OnPropertyChanged(nameof(Contacts));
        }

        private SessionTabViewModel? FindOrCreateTabForSession(SessionListItemViewModel sessionItem)
        {
            if (sessionItem.IsCurrent)
            {
                return CurrentSessionTab;
            }

            // Check if tab reference exists AND is still in Tabs collection
            if (sessionItem.Tab != null && Tabs.Contains(sessionItem.Tab))
            {
                return sessionItem.Tab;
            }

            // Check if tab already exists in Tabs collection by SessionKey
            var existingTab = Tabs.FirstOrDefault(t => t.SessionKey == sessionItem.SessionKey);
            if (existingTab != null)
            {
                sessionItem.Tab = existingTab;
                return existingTab;
            }

            // Create new tab for history session (will load on demand)
            if (sessionItem.SessionKey != null && _databaseService != null)
            {
                var session = new CallSession
                {
                    SessionKey = sessionItem.SessionKey,
                    ContactCount = sessionItem.ContactCount,
                    CreatedAt = sessionItem.CreatedAt.ToString("O")
                };
                var newTab = new SessionTabViewModel(session);
                sessionItem.Tab = newTab;
                Tabs.Add(newTab);
                return newTab;
            }

            return null;
        }

        /// <summary>
        /// Sync sidebar selection when tab changes (e.g., from TabControl click).
        /// </summary>
        private void SyncSidebarSelection(SessionTabViewModel tab)
        {
            if (_isSyncingSelection) return;
            
            var sessionItem = Sessions.FirstOrDefault(s =>
                (tab.IsCurrentSession && s.IsCurrent) ||
                (!tab.IsCurrentSession && s.SessionKey == tab.SessionKey));

            if (sessionItem != null && sessionItem != SelectedSession)
            {
                _isSyncingSelection = true;
                try
                {
                    SelectedSession = sessionItem;
                }
                finally
                {
                    _isSyncingSelection = false;
                }
            }
        }

        /// <summary>
        /// Refresh sessions list from database.
        /// </summary>
        [RelayCommand]
        private async Task RefreshSessions()
        {
            if (_databaseService == null) return;

            IsLoading = true;
            StatusMessage = "Đang tải sessions...";

            try
            {
                // Keep current session item
                var currentItem = Sessions.FirstOrDefault(s => s.IsCurrent);
                Sessions.Clear();

                if (currentItem != null)
                {
                    currentItem.Tab = CurrentSessionTab;
                    Sessions.Add(currentItem);
                }
                else
                {
                    Sessions.Add(new SessionListItemViewModel { Tab = CurrentSessionTab });
                }

                // Load history sessions
                var historySessions = await _databaseService.GetAllSessionsAsync();
                foreach (var session in historySessions.OrderByDescending(s => s.CreatedAt))
                {
                    var item = new SessionListItemViewModel(session);
                    // Link to existing tab if open
                    item.Tab = Tabs.FirstOrDefault(t => t.SessionKey == session.SessionKey);
                    Sessions.Add(item);
                }

                StatusMessage = $"Đã tải {Sessions.Count} sessions";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải sessions: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Delete a history session from sidebar.
        /// </summary>
        [RelayCommand]
        private void DeleteSession(SessionListItemViewModel? session)
        {
            if (session == null || session.IsCurrent) return;

            // Find corresponding tab to delete
            var tab = session.Tab ?? Tabs.FirstOrDefault(t => t.SessionKey == session.SessionKey);
            if (tab != null)
            {
                RequestDeleteSession(tab);
            }
        }

        /// <summary>
        /// Toggle sidebar visibility.
        /// </summary>
        [RelayCommand]
        private void ToggleSidebar()
        {
            IsSidebarVisible = !IsSidebarVisible;
        }

        /// <summary>
        /// Select a session from sidebar list.
        /// </summary>
        [RelayCommand]
        private void SelectSession(SessionListItemViewModel? session)
        {
            if (session == null) return;
            SelectedSession = session;
        }

        [RelayCommand]
        private async Task SaveSession()
        {
            if (_databaseService == null || CurrentSessionTab == null) return;
            if (CurrentSessionTab.AllContacts.Count == 0)
            {
                NotificationService.ShowWarning("Không có dữ liệu để lưu");
                return;
            }

            IsSaving = true;
            StatusMessage = "Dang luu...";

            try
            {
                var sessionKey = await _databaseService.GenerateUniqueSessionKeyAsync();
                var session = await _databaseService.CreateSessionAsync(sessionKey);
                await _databaseService.SaveContactsAsync(sessionKey, CurrentSessionTab.AllContacts);
                session.ContactCount = CurrentSessionTab.AllContacts.Count;

                // Create history tab with cloned data BEFORE clearing current
                var historyTab = new SessionTabViewModel(session);
                historyTab.SetContacts(CurrentSessionTab.CloneContacts());
                historyTab.IsLoaded = true;
                Tabs.Insert(1, historyTab);

                // Create sidebar item and add after current session
                var sidebarItem = new SessionListItemViewModel(session) { Tab = historyTab };
                Sessions.Insert(1, sidebarItem);

                // ═══════════════════════════════════════════════════════════════
                // CLEAR CURRENT SESSION - Reset to empty state
                // ═══════════════════════════════════════════════════════════════
                CurrentSessionTab.AllContacts.Clear();
                CurrentSessionTab.UpdateStatistics();
                
                // Select current session (back to empty state)
                var currentSessionItem = Sessions.FirstOrDefault(s => s.IsCurrent);
                SelectedTab = CurrentSessionTab;
                SelectedSession = currentSessionItem;
                
                UpdateStatisticsNotification();

                NotificationService.ShowSuccess($"Đã lưu session {session.FormattedDate}");
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                NotificationService.ShowError("Lỗi lưu session");
                StatusMessage = "Loi luu: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Save failed: " + ex);
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void CloseTab(SessionTabViewModel? tab)
        {
            if (tab == null || tab.IsCurrentSession) return;
            
            // Clear reference in sidebar item so it can be recreated
            var sidebarItem = Sessions.FirstOrDefault(s => s.SessionKey == tab.SessionKey);
            if (sidebarItem != null)
            {
                sidebarItem.Tab = null;
            }
            
            Tabs.Remove(tab);
            
            if (SelectedTab == tab)
            {
                SelectedTab = CurrentSessionTab;
                // Also sync sidebar selection
                var currentSessionItem = Sessions.FirstOrDefault(s => s.IsCurrent);
                if (currentSessionItem != null)
                {
                    SelectedSession = currentSessionItem;
                }
            }
        }

        [RelayCommand]
        private void RequestDeleteSession(SessionTabViewModel? tab)
        {
            if (tab == null || tab.IsCurrentSession) return;
            SessionToDelete = tab;
            ShowDeleteConfirmation = true;
        }

        [RelayCommand]
        private async Task ConfirmDeleteSession()
        {
            if (SessionToDelete == null || _databaseService == null) return;

            try
            {
                StatusMessage = "Dang xoa...";
                await _databaseService.DeleteSessionAsync(SessionToDelete.SessionKey!);
                
                // Remove from tabs
                Tabs.Remove(SessionToDelete);

                // Remove from sidebar
                var sidebarItem = Sessions.FirstOrDefault(s => s.SessionKey == SessionToDelete.SessionKey);
                if (sidebarItem != null)
                {
                    Sessions.Remove(sidebarItem);
                }

                if (SelectedTab == SessionToDelete)
                {
                    SelectedTab = CurrentSessionTab;
                    SelectedSession = Sessions.FirstOrDefault(s => s.IsCurrent);
                }
                
                NotificationService.ShowSuccess("Đã xóa session");
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                NotificationService.ShowError("Lỗi xóa session");
                StatusMessage = "Loi xoa: " + ex.Message;
            }
            finally
            {
                ShowDeleteConfirmation = false;
                SessionToDelete = null;
            }
        }

        [RelayCommand]
        private void CancelDeleteSession()
        {
            ShowDeleteConfirmation = false;
            SessionToDelete = null;
        }

        [RelayCommand]
        private void ClearCurrentSession()
        {
            if (CurrentSessionTab == null) return;
            CurrentSessionTab.AllContacts.Clear();
            CurrentSessionTab.UpdateStatistics();
            UpdateStatisticsNotification();
            NotificationService.ShowInfo("Đã xóa dữ liệu phiên hiện tại");
        }

        [RelayCommand]
        private async Task ImportExcel()
        {
            if (MainWindow == null || CurrentSessionTab == null) return;

            try
            {
                // Open file picker
                var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Chọn file Excel để import",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Excel Files")
                        {
                            Patterns = new[] { "*.xlsx" },
                            MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
                        }
                    }
                };

                var files = await MainWindow.StorageProvider.OpenFilePickerAsync(dialog);

                if (files == null || files.Count == 0)
                {
                    return; // User cancelled
                }

                var filePath = files[0].Path.LocalPath;

                IsLoading = true;
                StatusMessage = "Đang import...";

                // Import contacts
                var result = await _excelService.ImportAsync(filePath);

                if (!result.IsSuccess)
                {
                    NotificationService.ShowError(result.ErrorMessage ?? "Import thất bại");
                    return;
                }

                // Add contacts to current session
                foreach (var contact in result.Contacts)
                {
                    // Assign new ID based on existing contacts
                    contact.Id = CurrentSessionTab.AllContacts.Count + 1;
                    CurrentSessionTab.AllContacts.Add(contact);
                }

                CurrentSessionTab.UpdateStatistics();
                UpdateStatisticsNotification();

                NotificationService.ShowSuccess($"Đã import {result.Contacts.Count} liên hệ");
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                NotificationService.ShowError("Lỗi import file Excel");
                System.Diagnostics.Debug.WriteLine($"Import error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ExportExcel()
        {
            if (MainWindow == null || SelectedTab == null) return;

            if (SelectedTab.AllContacts.Count == 0)
            {
                NotificationService.ShowWarning("Không có dữ liệu để xuất");
                return;
            }

            try
            {
                // Generate default filename
                var defaultFileName = _excelService.GenerateExportFilename();

                // Open save file picker
                var dialog = new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Lưu file Excel",
                    SuggestedFileName = defaultFileName,
                    DefaultExtension = "xlsx",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Excel Files")
                        {
                            Patterns = new[] { "*.xlsx" },
                            MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
                        }
                    }
                };

                var file = await MainWindow.StorageProvider.SaveFilePickerAsync(dialog);

                if (file == null)
                {
                    return; // User cancelled
                }

                var filePath = file.Path.LocalPath;

                IsLoading = true;
                StatusMessage = "Đang xuất Excel...";

                // Export contacts (use AllContacts for full data)
                var sessionName = SelectedTab.IsCurrentSession 
                    ? "Current Session" 
                    : SelectedTab.Session?.FormattedDate ?? "Session";

                await _excelService.ExportAsync(filePath, SelectedTab.AllContacts, sessionName);

                NotificationService.ShowSuccess($"Đã xuất {SelectedTab.AllContacts.Count} liên hệ");
                StatusMessage = string.Empty;
            }
            catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("access"))
            {
                NotificationService.ShowError("File đang được mở. Vui lòng đóng và thử lại");
            }
            catch (Exception ex)
            {
                NotificationService.ShowError("Lỗi xuất file Excel");
                System.Diagnostics.Debug.WriteLine($"Export error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CopyPhoneNumber(Contact? contact)
        {
            if (contact == null || string.IsNullOrEmpty(contact.PhoneNumber)) return;

            try
            {
                var clipboard = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow?.Clipboard
                    : null;

                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(contact.PhoneNumber);
                    NotificationService.ShowSuccess($"Đã copy: {contact.PhoneNumber}");
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError("Lỗi copy số điện thoại");
                System.Diagnostics.Debug.WriteLine($"Copy error: {ex}");
            }
        }

        [RelayCommand]
        private void SetInterested(Contact? contact)
        {
            if (contact == null || SelectedTab == null) return;
            SelectedTab.SetInterestedCommand.Execute(contact);
            UpdateStatisticsNotification();
            NotificationService.ShowSuccess($"✓ {contact.Name}: Có nhu cầu");
        }

        [RelayCommand]
        private void SetNotInterested(Contact? contact)
        {
            if (contact == null || SelectedTab == null) return;
            SelectedTab.SetNotInterestedCommand.Execute(contact);
            UpdateStatisticsNotification();
            NotificationService.ShowInfo($"✓ {contact.Name}: Không nhu cầu");
        }

        [RelayCommand]
        private void SetNoAnswer(Contact? contact)
        {
            if (contact == null || SelectedTab == null) return;
            SelectedTab.SetNoAnswerCommand.Execute(contact);
            UpdateStatisticsNotification();
            NotificationService.ShowInfo($"✓ {contact.Name}: Không bắt máy");
        }

        [RelayCommand]
        private void SetInvalidNumber(Contact? contact)
        {
            if (contact == null || SelectedTab == null) return;
            SelectedTab.SetInvalidNumberCommand.Execute(contact);
            UpdateStatisticsNotification();
            NotificationService.ShowWarning($"✓ {contact.Name}: Số không tồn tại");
        }

        [RelayCommand]
        private void SetBusy(Contact? contact)
        {
            if (contact == null || SelectedTab == null) return;
            SelectedTab.SetBusyCommand.Execute(contact);
            UpdateStatisticsNotification();
            NotificationService.ShowInfo($"✓ {contact.Name}: Máy bận");
        }

        [RelayCommand]
        private void ResetStatus(Contact? contact)
        {
            if (contact == null || SelectedTab == null) return;
            SelectedTab.ResetStatusCommand.Execute(contact);
            UpdateStatisticsNotification();
        }

        private void UpdateStatisticsNotification()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(InterestedCount));
            OnPropertyChanged(nameof(NotInterestedCount));
            OnPropertyChanged(nameof(NoAnswerCount));
            OnPropertyChanged(nameof(BusyCount));
            OnPropertyChanged(nameof(InvalidCount));
        }

        [RelayCommand]
        private void StartOnboarding()
        {
            CurrentOnboardingStep = 1;
            ShowOnboarding = true;
        }

        [RelayCommand]
        private void NextOnboardingStep()
        {
            if (CurrentOnboardingStep < TotalOnboardingSteps)
            {
                CurrentOnboardingStep++;
            }
            else
            {
                CloseOnboarding();
            }
        }

        [RelayCommand]
        private void PreviousOnboardingStep()
        {
            if (CurrentOnboardingStep > 1)
            {
                CurrentOnboardingStep--;
            }
        }

        [RelayCommand]
        private void SkipOnboarding()
        {
            CloseOnboarding();
        }

        [RelayCommand]
        private void DontShowAgain()
        {
            IsFirstLaunch = false;
            CloseOnboarding();
        }

        private void CloseOnboarding()
        {
            ShowOnboarding = false;
            CurrentOnboardingStep = 1;
        }

        [RelayCommand]
        private void ToggleHelp()
        {
            ShowHelpTooltips = !ShowHelpTooltips;
        }

        [RelayCommand]
        private void ShowHelp()
        {
            StartOnboarding();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SETTINGS COMMANDS
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task OpenSettings()
        {
            if (SettingsViewModel == null)
            {
                SettingsViewModel = new SettingsViewModel();
                
                // Initialize settings service
                var settingsService = new SettingsService();
                await SettingsViewModel.InitializeAsync();
            }
            
            ShowSettings = true;
        }

        [RelayCommand]
        private void CloseSettings()
        {
            ShowSettings = false;
            
            // Lock settings when closing (security requirement)
            if (SettingsViewModel != null)
            {
                SettingsViewModel.IsUnlocked = false;
            }
            
            // Check Telegram configuration after closing settings
            _ = CheckTelegramConfigurationAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SEND REPORT COMMANDS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Check if Telegram is configured and update the button state.
        /// </summary>
        public async Task CheckTelegramConfigurationAsync()
        {
            if (_databaseService == null) return;

            try
            {
                var telegramService = new TelegramReportService(_databaseService);
                IsTelegramConfigured = await telegramService.IsConfiguredAsync();
            }
            catch
            {
                IsTelegramConfigured = false;
            }
        }

        /// <summary>
        /// Load the auto-send daily report setting from database.
        /// </summary>
        private async Task LoadAutoSendDailyReportSettingAsync()
        {
            if (SettingsService.Instance == null) return;

            try
            {
                var settings = await SettingsService.Instance.LoadDailyReportSettingsAsync();
                IsAutoSendDailyReportEnabled = settings.IsEnabled;
            }
            catch
            {
                IsAutoSendDailyReportEnabled = false;
            }
        }

        /// <summary>
        /// Handle daily report settings changed from Settings screen.
        /// </summary>
        private void OnDailyReportSettingsChanged(object? sender, SettingsService.DailyReportSettings settings)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsAutoSendDailyReportEnabled = settings.IsEnabled;
            });
        }

        /// <summary>
        /// Handle auto-sent daily report event - show toast notification.
        /// </summary>
        private void OnDailyReportAutoSent(object? sender, DailyReportSentEventArgs e)
        {
            if (e.IsSuccess)
            {
                NotificationService.ShowSuccess($"✅ Auto Daily Report: {e.Message}");
            }
            else
            {
                NotificationService.ShowError($"❌ Auto Daily Report failed: {e.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenSendReport()
        {
            if (_databaseService == null) return;

            // Initialize SendReportViewModel
            SendReportViewModel = new SendReportViewModel();
            SendReportViewModel.RequestClose += () =>
            {
                ShowSendReport = false;
            };

            await SendReportViewModel.InitializeAsync(_databaseService);
            ShowSendReport = true;
        }

        [RelayCommand]
        private async Task SendDailyReport()
        {
            if (_databaseService == null) return;

            IsLoading = true;
            StatusMessage = "Đang tải preview Daily Report...";

            try
            {
                // Create and initialize the preview ViewModel
                DailyReportPreviewViewModel = new DailyReportPreviewViewModel(_databaseService);
                
                // Subscribe to events
                DailyReportPreviewViewModel.CloseRequested += () =>
                {
                    ShowDailyReportPreview = false;
                };

                DailyReportPreviewViewModel.ReportSent += (result) =>
                {
                    NotificationService.ShowSuccess($"Daily Report đã gửi thành công! ({result.TotalCalls} cuộc gọi, {result.InterestedCount} có nhu cầu)");
                };

                // Load preview content
                await DailyReportPreviewViewModel.LoadPreviewAsync(DateTime.Today);

                // Show the preview popup
                ShowDailyReportPreview = true;
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Lỗi: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SendDailyReport error: {ex}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Send daily report directly without preview (for auto-send scheduled task).
        /// </summary>
        public async Task SendDailyReportDirectAsync(DateTime date)
        {
            if (_databaseService == null) return;

            try
            {
                using var reportService = new DailyReportService(_databaseService);
                
                // Validate configuration first
                if (!await reportService.IsConfiguredAsync())
                {
                    NotificationService.ShowError("Telegram chưa được cấu hình cho auto-send.");
                    return;
                }

                var result = await reportService.SendDailyReportAsync(date);

                if (result.IsSuccess)
                {
                    NotificationService.ShowSuccess($"Auto Daily Report đã gửi! ({result.TotalCalls} cuộc gọi)");
                }
                else
                {
                    NotificationService.ShowError($"Auto Daily Report thất bại: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Lỗi auto-send: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SendDailyReportDirectAsync error: {ex}");
            }
        }

        [RelayCommand]
        private void CloseDailyReportPreview()
        {
            ShowDailyReportPreview = false;
        }

        [RelayCommand]
        private void CloseSendReport()
        {
            ShowSendReport = false;
        }

        private void CheckFirstLaunch()
        {
            if (IsFirstLaunch)
            {
                ShowOnboarding = true;
            }
        }

        private void LoadSampleData()
        {
            if (CurrentSessionTab == null) return;

            var sampleContacts = new ObservableCollection<Contact>
            {
                new Contact(1, "Nguyen Van An", "0901234567", "Cong ty ABC", "Khach hang tiem nang"),
                new Contact(2, "Tran Thi Binh", "0912345678", "Cong ty XYZ", ""),
                new Contact(3, "Le Van Cuong", "0923456789", "Cong ty DEF", "Goi lai sau 2 ngay"),
                new Contact(4, "Pham Thi Dung", "0934567890", "Cong ty GHI", ""),
                new Contact(5, "Hoang Van Em", "0945678901", "Cong ty JKL", "Khong co nhu cau"),
                new Contact(6, "Vu Thi Phuong", "0956789012", "Cong ty MNO", ""),
                new Contact(7, "Dang Van Giang", "0967890123", "Cong ty PQR", ""),
                new Contact(8, "Bui Thi Huong", "0978901234", "Cong ty STU", "Hen gap tuan sau"),
                new Contact(9, "Ngo Van Inh", "0989012345", "Cong ty VWX", ""),
                new Contact(10, "Duong Thi Kim", "0990123456", "Cong ty YZA", ""),
            };

            CurrentSessionTab.SetContacts(sampleContacts);
            UpdateStatisticsNotification();
        }

        public string OnboardingStep1Title => "Import danh sach";
        public string OnboardingStep1Description => "Nhan nut Import Excel de tai len danh sach so dien thoai can goi.";

        public string OnboardingStep2Title => "Danh dau trang thai";
        public string OnboardingStep2Description => "Click vao cac nut trang thai de danh dau ket qua cuoc goi.";

        public string OnboardingStep3Title => "Luu & Xuat ket qua";
        public string OnboardingStep3Description => "Nhan Save Session de luu lai, hoac Export Excel de xuat file.";
    }
}
