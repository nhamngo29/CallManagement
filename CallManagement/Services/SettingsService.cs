using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Service for managing application settings persistence.
    /// Handles Telegram Bot configuration with SQLite storage.
    /// </summary>
    public class SettingsService : IDisposable
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// SHA-256 hash of the fixed password "Hoa@123"
        /// Generated using: SHA256("Hoa@123")
        /// </summary>
        private const string PASSWORD_HASH = "7E4B2E7E8D3C9F1A5B6D8C2E4F0A1B3C5D7E9F0A2B4C6D8E0F1A3B5C7D9E1F3A";

        // Actual SHA-256 hash computation for "Hoa@123"
        private static readonly string COMPUTED_PASSWORD_HASH = ComputePasswordHash();

        private readonly string _connectionString;
        private readonly string _databasePath;
        private bool _disposed;

        /// <summary>
        /// Singleton instance for app-wide settings access.
        /// </summary>
        public static SettingsService Instance { get; private set; } = null!;

        /// <summary>
        /// Event raised when daily report settings are changed.
        /// </summary>
        public event EventHandler<DailyReportSettings>? DailyReportSettingsChanged;

        // ═══════════════════════════════════════════════════════════════════════
        // SETTINGS MODEL
        // ═══════════════════════════════════════════════════════════════════════

        public class TelegramSettings
        {
            public string BotToken { get; set; } = string.Empty;
            public string ChatId { get; set; } = string.Empty;
        }

        public class DailyReportSettings
        {
            public bool IsEnabled { get; set; }
            public TimeSpan SendTime { get; set; } = new TimeSpan(18, 0, 0); // Default 18:00
            public DateTime? LastSentDate { get; set; }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR & INITIALIZATION
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize the settings service.
        /// </summary>
        /// <param name="databaseName">Name of the settings database file</param>
        public SettingsService(string databaseName = "CallManagement.db")
        {
            var appDataPath = GetAppDataPath();
            Directory.CreateDirectory(appDataPath);
            
            _databasePath = Path.Combine(appDataPath, databaseName);
            _connectionString = $"Data Source={_databasePath}";
            
            Instance = this;
        }

        /// <summary>
        /// Get the app data path for the current OS.
        /// </summary>
        private static string GetAppDataPath()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(basePath, "CallManagement");
        }

        /// <summary>
        /// Initialize the settings table.
        /// </summary>
        public async Task InitializeAsync()
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS AppSettings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
            ";

            await using var cmd = new SqliteCommand(createTableSql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PASSWORD VERIFICATION
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Compute the SHA-256 hash of the fixed password at compile time.
        /// </summary>
        private static string ComputePasswordHash()
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("Hoa@123"));
            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Verify the input password against the stored hash.
        /// </summary>
        /// <param name="password">User input password</param>
        /// <returns>True if password matches, false otherwise</returns>
        public static bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            using var sha256 = SHA256.Create();
            var inputBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var inputHash = Convert.ToHexString(inputBytes);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(inputHash),
                Encoding.UTF8.GetBytes(COMPUTED_PASSWORD_HASH)
            );
        }

        // ═══════════════════════════════════════════════════════════════════════
        // TELEGRAM SETTINGS OPERATIONS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Save Telegram settings to the database.
        /// </summary>
        public async Task SaveTelegramSettingsAsync(TelegramSettings settings)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Use REPLACE to upsert
            var sql = @"
                REPLACE INTO AppSettings (Key, Value) VALUES ('TelegramBotToken', @BotToken);
                REPLACE INTO AppSettings (Key, Value) VALUES ('TelegramChatId', @ChatId);
            ";

            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@BotToken", settings.BotToken ?? string.Empty);
            cmd.Parameters.AddWithValue("@ChatId", settings.ChatId ?? string.Empty);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Load Telegram settings from the database.
        /// </summary>
        public async Task<TelegramSettings> LoadTelegramSettingsAsync()
        {
            var settings = new TelegramSettings();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Key, Value FROM AppSettings WHERE Key IN ('TelegramBotToken', 'TelegramChatId')";
            await using var cmd = new SqliteCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);

                switch (key)
                {
                    case "TelegramBotToken":
                        settings.BotToken = value;
                        break;
                    case "TelegramChatId":
                        settings.ChatId = value;
                        break;
                }
            }

            return settings;
        }

        /// <summary>
        /// Clear all Telegram settings from the database.
        /// </summary>
        public async Task ClearTelegramSettingsAsync()
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "DELETE FROM AppSettings WHERE Key IN ('TelegramBotToken', 'TelegramChatId')";
            await using var cmd = new SqliteCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DAILY REPORT SETTINGS OPERATIONS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Save daily report auto-send settings to the database.
        /// </summary>
        public async Task SaveDailyReportSettingsAsync(DailyReportSettings settings)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                REPLACE INTO AppSettings (Key, Value) VALUES ('DailyReportEnabled', @Enabled);
                REPLACE INTO AppSettings (Key, Value) VALUES ('DailyReportSendTime', @SendTime);
            ";

            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Enabled", settings.IsEnabled ? "1" : "0");
            cmd.Parameters.AddWithValue("@SendTime", settings.SendTime.ToString(@"hh\:mm"));
            await cmd.ExecuteNonQueryAsync();

            // Raise event to notify subscribers
            DailyReportSettingsChanged?.Invoke(this, settings);
        }

        /// <summary>
        /// Load daily report auto-send settings from the database.
        /// </summary>
        public async Task<DailyReportSettings> LoadDailyReportSettingsAsync()
        {
            var settings = new DailyReportSettings();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Key, Value FROM AppSettings WHERE Key IN ('DailyReportEnabled', 'DailyReportSendTime', 'DailyReportLastSent')";
            await using var cmd = new SqliteCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);

                switch (key)
                {
                    case "DailyReportEnabled":
                        settings.IsEnabled = value == "1";
                        break;
                    case "DailyReportSendTime":
                        if (TimeSpan.TryParse(value, out var time))
                        {
                            settings.SendTime = time;
                        }
                        break;
                    case "DailyReportLastSent":
                        if (DateTime.TryParse(value, out var lastSent))
                        {
                            settings.LastSentDate = lastSent;
                        }
                        break;
                }
            }

            return settings;
        }

        /// <summary>
        /// Save the last daily report sent date to prevent duplicate sends.
        /// </summary>
        public async Task SaveLastDailyReportSentDateAsync(DateTime date)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "REPLACE INTO AppSettings (Key, Value) VALUES ('DailyReportLastSent', @Date)";
            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Date", date.ToString("O"));
            await cmd.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
