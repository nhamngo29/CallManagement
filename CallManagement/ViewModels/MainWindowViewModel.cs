using Avalonia;
using CallManagement.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CallManagement.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // OBSERVABLE PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Collection of contacts to display in the DataGrid.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Contact> _contacts = new();

        /// <summary>
        /// Currently selected contact in the DataGrid.
        /// </summary>
        [ObservableProperty]
        private Contact? _selectedContact;

        /// <summary>
        /// Search/filter text.
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Indicates if this is the first launch (for onboarding).
        /// </summary>
        [ObservableProperty]
        private bool _isFirstLaunch = true;

        /// <summary>
        /// Indicates if the onboarding overlay should be shown.
        /// </summary>
        [ObservableProperty]
        private bool _showOnboarding = false;

        /// <summary>
        /// Current onboarding step (1-based).
        /// </summary>
        [ObservableProperty]
        private int _currentOnboardingStep = 1;

        /// <summary>
        /// Total number of onboarding steps.
        /// </summary>
        public int TotalOnboardingSteps => 3;

        /// <summary>
        /// Indicates if help tooltips should be shown.
        /// </summary>
        [ObservableProperty]
        private bool _showHelpTooltips = false;

        /// <summary>
        /// Statistics - Total contacts count.
        /// </summary>
        [ObservableProperty]
        private int _totalCount;

        /// <summary>
        /// Statistics - Answered calls count.
        /// </summary>
        [ObservableProperty]
        private int _answeredCount;

        /// <summary>
        /// Statistics - No answer calls count.
        /// </summary>
        [ObservableProperty]
        private int _noAnswerCount;

        /// <summary>
        /// Statistics - Invalid numbers count.
        /// </summary>
        [ObservableProperty]
        private int _invalidCount;

        /// <summary>
        /// Statistics - Busy calls count.
        /// </summary>
        [ObservableProperty]
        private int _busyCount;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════

        public MainWindowViewModel()
        {
            // Load sample data for demonstration
            LoadSampleData();

            // Check if first launch (in real app, this would be persisted)
            CheckFirstLaunch();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // COMMANDS - Import/Export
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void ImportExcel()
        {
            // TODO: Implement Excel import functionality
            // This would open a file picker and parse the Excel file
            System.Diagnostics.Debug.WriteLine("Import Excel clicked");
        }

        [RelayCommand]
        private void ExportExcel()
        {
            // TODO: Implement Excel export functionality
            // This would save the current data to an Excel file
            System.Diagnostics.Debug.WriteLine("Export Excel clicked");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // COMMANDS - Copy Phone Number
        // ═══════════════════════════════════════════════════════════════════════

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
                    System.Diagnostics.Debug.WriteLine($"Copied: {contact.PhoneNumber}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Copy failed: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // COMMANDS - Call Status Actions
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void SetAnswered(Contact? contact)
        {
            if (contact == null) return;
            SetContactStatus(contact, CallStatus.Answered);
        }

        [RelayCommand]
        private void SetNoAnswer(Contact? contact)
        {
            if (contact == null) return;
            SetContactStatus(contact, CallStatus.NoAnswer);
        }

        [RelayCommand]
        private void SetInvalidNumber(Contact? contact)
        {
            if (contact == null) return;
            SetContactStatus(contact, CallStatus.InvalidNumber);
        }

        [RelayCommand]
        private void SetBusy(Contact? contact)
        {
            if (contact == null) return;
            SetContactStatus(contact, CallStatus.Busy);
        }

        [RelayCommand]
        private void ResetStatus(Contact? contact)
        {
            if (contact == null) return;
            contact.Status = CallStatus.None;
            contact.IsActionsEnabled = true;
            UpdateStatistics();
        }

        private void SetContactStatus(Contact contact, CallStatus status)
        {
            contact.Status = status;
            contact.IsActionsEnabled = false;
            UpdateStatistics();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // COMMANDS - Onboarding
        // ═══════════════════════════════════════════════════════════════════════

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
            // TODO: Persist this setting
            CloseOnboarding();
        }

        private void CloseOnboarding()
        {
            ShowOnboarding = false;
            CurrentOnboardingStep = 1;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // COMMANDS - Help
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void ToggleHelp()
        {
            ShowHelpTooltips = !ShowHelpTooltips;
        }

        [RelayCommand]
        private void ShowHelp()
        {
            // Show help dialog or start onboarding again
            StartOnboarding();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════════════

        private void CheckFirstLaunch()
        {
            // In a real app, check persisted settings
            if (IsFirstLaunch)
            {
                ShowOnboarding = true;
            }
        }

        private void UpdateStatistics()
        {
            TotalCount = Contacts.Count;
            AnsweredCount = Contacts.Count(c => c.Status == CallStatus.Answered);
            NoAnswerCount = Contacts.Count(c => c.Status == CallStatus.NoAnswer);
            InvalidCount = Contacts.Count(c => c.Status == CallStatus.InvalidNumber);
            BusyCount = Contacts.Count(c => c.Status == CallStatus.Busy);
        }

        private void LoadSampleData()
        {
            // Sample data for demonstration
            Contacts = new ObservableCollection<Contact>
            {
                new Contact(1, "Nguyễn Văn An", "0901234567", "Công ty ABC"),
                new Contact(2, "Trần Thị Bình", "0912345678", "Công ty XYZ"),
                new Contact(3, "Lê Văn Cường", "0923456789", "Công ty DEF"),
                new Contact(4, "Phạm Thị Dung", "0934567890", "Công ty GHI"),
                new Contact(5, "Hoàng Văn Em", "0945678901", "Công ty JKL"),
                new Contact(6, "Vũ Thị Phương", "0956789012", "Công ty MNO"),
                new Contact(7, "Đặng Văn Giang", "0967890123", "Công ty PQR"),
                new Contact(8, "Bùi Thị Hương", "0978901234", "Công ty STU"),
                new Contact(9, "Ngô Văn Inh", "0989012345", "Công ty VWX"),
                new Contact(10, "Dương Thị Kim", "0990123456", "Công ty YZA"),
            };

            UpdateStatistics();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ONBOARDING CONTENT (for localization-ready design)
        // ═══════════════════════════════════════════════════════════════════════

        public string OnboardingStep1Title => "Import danh sách";
        public string OnboardingStep1Description => "Nhấn nút Import Excel để tải lên danh sách số điện thoại cần gọi.";

        public string OnboardingStep2Title => "Đánh dấu trạng thái";
        public string OnboardingStep2Description => "Click vào các nút trạng thái để đánh dấu kết quả cuộc gọi.";

        public string OnboardingStep3Title => "Xuất kết quả";
        public string OnboardingStep3Description => "Nhấn Export Excel để lưu kết quả cuộc gọi ra file.";
    }
}
