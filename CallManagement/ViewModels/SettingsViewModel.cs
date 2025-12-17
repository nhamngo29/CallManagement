using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CallManagement.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings screen.
    /// Manages password-protected access to Telegram Bot configuration.
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PASSWORD SECTION
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private string _passwordInput = string.Empty;

        [ObservableProperty]
        private bool _isUnlocked;

        [ObservableProperty]
        private string _passwordError = string.Empty;

        [ObservableProperty]
        private bool _showPasswordError;

        // ═══════════════════════════════════════════════════════════════════════
        // TELEGRAM SETTINGS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private string _telegramBotToken = string.Empty;

        [ObservableProperty]
        private string _telegramChatId = string.Empty;

        [ObservableProperty]
        private string _botTokenError = string.Empty;

        [ObservableProperty]
        private string _chatIdError = string.Empty;

        [ObservableProperty]
        private bool _showBotTokenError;

        [ObservableProperty]
        private bool _showChatIdError;

        // ═══════════════════════════════════════════════════════════════════════
        // STATUS & FEEDBACK
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _showStatusMessage;

        [ObservableProperty]
        private bool _isStatusSuccess;

        private SettingsService? _settingsService;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR & INITIALIZATION
        // ═══════════════════════════════════════════════════════════════════════

        public SettingsViewModel()
        {
        }

        /// <summary>
        /// Initialize the ViewModel and load existing settings.
        /// </summary>
        public async Task InitializeAsync()
        {
            _settingsService = SettingsService.Instance;
            
            if (_settingsService != null)
            {
                await _settingsService.InitializeAsync();
                await LoadSettingsAsync();
            }
        }

        /// <summary>
        /// Load existing Telegram settings from storage.
        /// </summary>
        private async Task LoadSettingsAsync()
        {
            if (_settingsService == null) return;

            try
            {
                var settings = await _settingsService.LoadTelegramSettingsAsync();
                TelegramBotToken = settings.BotToken;
                TelegramChatId = settings.ChatId;
            }
            catch
            {
                // Silently fail - settings will be empty
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UNLOCK COMMAND
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void Unlock()
        {
            ShowPasswordError = false;
            
            if (string.IsNullOrWhiteSpace(PasswordInput))
            {
                PasswordError = "Vui lòng nhập mật khẩu";
                ShowPasswordError = true;
                return;
            }

            if (SettingsService.VerifyPassword(PasswordInput))
            {
                IsUnlocked = true;
                ShowPasswordError = false;
                
                // Clear password input for security
                PasswordInput = string.Empty;
                
                ShowSuccessStatus("Settings unlocked ✔️");
            }
            else
            {
                PasswordError = "Invalid password";
                ShowPasswordError = true;
                IsUnlocked = false;
                
                // Clear password input for security
                PasswordInput = string.Empty;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SAVE SETTINGS COMMAND
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!IsUnlocked || _settingsService == null)
                return;

            // Validate inputs
            if (!ValidateTelegramSettings())
                return;

            IsSaving = true;
            ShowStatusMessage = false;

            try
            {
                var settings = new SettingsService.TelegramSettings
                {
                    BotToken = TelegramBotToken.Trim(),
                    ChatId = TelegramChatId.Trim()
                };

                await _settingsService.SaveTelegramSettingsAsync(settings);
                
                ShowSuccessStatus("Đã lưu cài đặt thành công ✔️");
            }
            catch (Exception)
            {
                ShowErrorStatus("Lỗi khi lưu cài đặt. Vui lòng thử lại.");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Validate Telegram settings before saving.
        /// </summary>
        private bool ValidateTelegramSettings()
        {
            bool isValid = true;

            // Reset errors
            ShowBotTokenError = false;
            ShowChatIdError = false;

            // Validate Bot Token
            if (string.IsNullOrWhiteSpace(TelegramBotToken))
            {
                BotTokenError = "Bot Token không được để trống";
                ShowBotTokenError = true;
                isValid = false;
            }
            else if (!TelegramBotToken.Contains(':'))
            {
                BotTokenError = "Bot Token không hợp lệ (phải chứa dấu ':')";
                ShowBotTokenError = true;
                isValid = false;
            }

            // Validate Chat ID
            if (string.IsNullOrWhiteSpace(TelegramChatId))
            {
                ChatIdError = "Chat ID không được để trống";
                ShowChatIdError = true;
                isValid = false;
            }
            else if (!Regex.IsMatch(TelegramChatId.Trim(), @"^-?\d+$"))
            {
                ChatIdError = "Chat ID chỉ được chứa số";
                ShowChatIdError = true;
                isValid = false;
            }

            return isValid;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CLEAR SENSITIVE DATA COMMAND
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task ClearSensitiveDataAsync()
        {
            if (_settingsService == null) return;

            try
            {
                await _settingsService.ClearTelegramSettingsAsync();
                
                TelegramBotToken = string.Empty;
                TelegramChatId = string.Empty;
                
                ShowSuccessStatus("Đã xóa dữ liệu Telegram");
            }
            catch
            {
                ShowErrorStatus("Lỗi khi xóa dữ liệu");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // LOCK COMMAND (Re-lock settings)
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void Lock()
        {
            IsUnlocked = false;
            PasswordInput = string.Empty;
            ShowStatusMessage = false;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STATUS HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        private void ShowSuccessStatus(string message)
        {
            StatusMessage = message;
            IsStatusSuccess = true;
            ShowStatusMessage = true;
        }

        private void ShowErrorStatus(string message)
        {
            StatusMessage = message;
            IsStatusSuccess = false;
            ShowStatusMessage = true;
        }
    }
}
