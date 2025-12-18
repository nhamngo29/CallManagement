using CallManagement.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// SQLite database service for managing call sessions and contacts.
    /// Uses Microsoft.Data.Sqlite for cross-platform compatibility (Windows & macOS).
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private bool _disposed;

        /// <summary>
        /// Singleton instance for app-wide database access.
        /// </summary>
        public static DatabaseService Instance { get; private set; } = null!;

        /// <summary>
        /// Initialize the database service.
        /// </summary>
        /// <param name="databaseName">Name of the database file (without path)</param>
        public DatabaseService(string databaseName = "CallManagement.db")
        {
            // Get appropriate app data folder based on OS
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
            // Use LocalApplicationData for cross-platform compatibility
            // Windows: C:\Users\{user}\AppData\Local\CallManagement
            // macOS: /Users/{user}/.local/share/CallManagement
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(basePath, "CallManagement");
        }

        /// <summary>
        /// Get the full path to the database file.
        /// </summary>
        public string DatabasePath => _databasePath;

        /// <summary>
        /// Initialize the database - create tables if not exist.
        /// </summary>
        public async Task InitializeAsync()
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Create CallSession table
            var createSessionTableSql = @"
                CREATE TABLE IF NOT EXISTS CallSession (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionKey TEXT NOT NULL UNIQUE,
                    CreatedAt TEXT NOT NULL,
                    DisplayName TEXT,
                    ContactCount INTEGER DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS idx_session_key ON CallSession(SessionKey);
                CREATE INDEX IF NOT EXISTS idx_created_at ON CallSession(CreatedAt);
            ";

            await using var cmd1 = new SqliteCommand(createSessionTableSql, connection);
            await cmd1.ExecuteNonQueryAsync();

            // Create Contact table
            var createContactTableSql = @"
                CREATE TABLE IF NOT EXISTS Contact (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionKey TEXT NOT NULL,
                    OriginalId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    PhoneNumber TEXT NOT NULL,
                    Company TEXT,
                    Status INTEGER DEFAULT 0,
                    Note TEXT,
                    LastCalledAt TEXT,
                    FOREIGN KEY (SessionKey) REFERENCES CallSession(SessionKey) ON DELETE CASCADE
                );
                
                CREATE INDEX IF NOT EXISTS idx_contact_session ON Contact(SessionKey);
            ";

            await using var cmd2 = new SqliteCommand(createContactTableSql, connection);
            await cmd2.ExecuteNonQueryAsync();

            // Enable foreign keys
            await using var cmd3 = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            await cmd3.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SESSION OPERATIONS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Create a new session with the given key.
        /// </summary>
        public async Task<CallSession> CreateSessionAsync(string sessionKey, string? displayName = null)
        {
            var session = new CallSession
            {
                SessionKey = sessionKey,
                CreatedAt = DateTime.Now.ToString("O"), // ISO 8601 format
                DisplayName = displayName,
                ContactCount = 0
            };

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO CallSession (SessionKey, CreatedAt, DisplayName, ContactCount)
                VALUES (@SessionKey, @CreatedAt, @DisplayName, @ContactCount);
                SELECT last_insert_rowid();
            ";

            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@SessionKey", session.SessionKey);
            cmd.Parameters.AddWithValue("@CreatedAt", session.CreatedAt);
            cmd.Parameters.AddWithValue("@DisplayName", session.DisplayName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactCount", session.ContactCount);

            var result = await cmd.ExecuteScalarAsync();
            session.Id = Convert.ToInt32(result);

            return session;
        }

        /// <summary>
        /// Get all sessions ordered by creation date (newest first).
        /// </summary>
        public async Task<List<CallSession>> GetAllSessionsAsync()
        {
            var sessions = new List<CallSession>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM CallSession ORDER BY CreatedAt DESC";
            await using var cmd = new SqliteCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sessions.Add(new CallSession
                {
                    Id = reader.GetInt32(0),
                    SessionKey = reader.GetString(1),
                    CreatedAt = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ContactCount = reader.GetInt32(4)
                });
            }

            return sessions;
        }

        /// <summary>
        /// Get a session by its key.
        /// </summary>
        public async Task<CallSession?> GetSessionByKeyAsync(string sessionKey)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM CallSession WHERE SessionKey = @SessionKey";
            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@SessionKey", sessionKey);
            
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CallSession
                {
                    Id = reader.GetInt32(0),
                    SessionKey = reader.GetString(1),
                    CreatedAt = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ContactCount = reader.GetInt32(4)
                };
            }

            return null;
        }

        /// <summary>
        /// Update session contact count.
        /// </summary>
        public async Task UpdateSessionContactCountAsync(string sessionKey, int count)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "UPDATE CallSession SET ContactCount = @Count WHERE SessionKey = @SessionKey";
            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Count", count);
            cmd.Parameters.AddWithValue("@SessionKey", sessionKey);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Delete a session and all its contacts.
        /// </summary>
        public async Task DeleteSessionAsync(string sessionKey)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Enable foreign keys for cascade delete
            await using var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            await pragmaCmd.ExecuteNonQueryAsync();

            // Delete contacts first (for safety)
            var deleteContactsSql = "DELETE FROM Contact WHERE SessionKey = @SessionKey";
            await using var cmd1 = new SqliteCommand(deleteContactsSql, connection);
            cmd1.Parameters.AddWithValue("@SessionKey", sessionKey);
            await cmd1.ExecuteNonQueryAsync();

            // Delete session
            var deleteSessionSql = "DELETE FROM CallSession WHERE SessionKey = @SessionKey";
            await using var cmd2 = new SqliteCommand(deleteSessionSql, connection);
            cmd2.Parameters.AddWithValue("@SessionKey", sessionKey);
            await cmd2.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Check if a session key already exists.
        /// </summary>
        public async Task<bool> SessionExistsAsync(string sessionKey)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT COUNT(*) FROM CallSession WHERE SessionKey = @SessionKey";
            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@SessionKey", sessionKey);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CONTACT OPERATIONS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Save contacts for a session.
        /// </summary>
        public async Task SaveContactsAsync(string sessionKey, IEnumerable<Contact> contacts)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            
            try
            {
                var sql = @"
                    INSERT INTO Contact (SessionKey, OriginalId, Name, PhoneNumber, Company, Status, Note, LastCalledAt)
                    VALUES (@SessionKey, @OriginalId, @Name, @PhoneNumber, @Company, @Status, @Note, @LastCalledAt)
                ";

                int count = 0;
                foreach (var contact in contacts)
                {
                    await using var cmd = new SqliteCommand(sql, connection, transaction);
                    cmd.Parameters.AddWithValue("@SessionKey", sessionKey);
                    cmd.Parameters.AddWithValue("@OriginalId", contact.Id);
                    cmd.Parameters.AddWithValue("@Name", contact.Name);
                    cmd.Parameters.AddWithValue("@PhoneNumber", contact.PhoneNumber);
                    cmd.Parameters.AddWithValue("@Company", contact.Company ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Status", (int)contact.Status);
                    cmd.Parameters.AddWithValue("@Note", contact.Note ?? string.Empty);
                    cmd.Parameters.AddWithValue("@LastCalledAt", contact.LastCalledAt.HasValue 
                        ? contact.LastCalledAt.Value.ToString("O") 
                        : (object)DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                    count++;
                }

                // Update session contact count
                var updateSql = "UPDATE CallSession SET ContactCount = @Count WHERE SessionKey = @SessionKey";
                await using var updateCmd = new SqliteCommand(updateSql, connection, transaction);
                updateCmd.Parameters.AddWithValue("@Count", count);
                updateCmd.Parameters.AddWithValue("@SessionKey", sessionKey);
                await updateCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Get all contacts for a session.
        /// </summary>
        public async Task<List<ContactEntity>> GetContactsBySessionAsync(string sessionKey)
        {
            var contacts = new List<ContactEntity>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM Contact WHERE SessionKey = @SessionKey ORDER BY OriginalId";
            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@SessionKey", sessionKey);
            
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                contacts.Add(new ContactEntity
                {
                    Id = reader.GetInt32(0),
                    SessionKey = reader.GetString(1),
                    OriginalId = reader.GetInt32(2),
                    Name = reader.GetString(3),
                    PhoneNumber = reader.GetString(4),
                    Company = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Status = reader.GetInt32(6),
                    Note = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    LastCalledAt = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }

            return contacts;
        }

        /// <summary>
        /// Update a single contact.
        /// </summary>
        public async Task UpdateContactAsync(ContactEntity contact)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE Contact 
                SET Name = @Name, PhoneNumber = @PhoneNumber, Company = @Company, 
                    Status = @Status, Note = @Note, LastCalledAt = @LastCalledAt
                WHERE Id = @Id
            ";

            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", contact.Id);
            cmd.Parameters.AddWithValue("@Name", contact.Name);
            cmd.Parameters.AddWithValue("@PhoneNumber", contact.PhoneNumber);
            cmd.Parameters.AddWithValue("@Company", contact.Company);
            cmd.Parameters.AddWithValue("@Status", contact.Status);
            cmd.Parameters.AddWithValue("@Note", contact.Note);
            cmd.Parameters.AddWithValue("@LastCalledAt", contact.LastCalledAt ?? (object)DBNull.Value);
            
            await cmd.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UTILITY METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generate a unique session key based on current timestamp.
        /// Format: ddMMyyHHmm (e.g., "1512251430")
        /// </summary>
        public static string GenerateSessionKey()
        {
            return DateTime.Now.ToString("ddMMyyHHmm");
        }

        /// <summary>
        /// Generate a unique session key, ensuring it doesn't exist in DB.
        /// </summary>
        public async Task<string> GenerateUniqueSessionKeyAsync()
        {
            var baseKey = GenerateSessionKey();
            var key = baseKey;
            var suffix = 1;

            while (await SessionExistsAsync(key))
            {
                key = $"{baseKey}_{suffix}";
                suffix++;
            }

            return key;
        }

        /// <summary>
        /// Get database statistics.
        /// </summary>
        public async Task<(int sessionCount, int contactCount)> GetStatisticsAsync()
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd1 = new SqliteCommand("SELECT COUNT(*) FROM CallSession", connection);
            var sessionCount = Convert.ToInt32(await cmd1.ExecuteScalarAsync());

            await using var cmd2 = new SqliteCommand("SELECT COUNT(*) FROM Contact", connection);
            var contactCount = Convert.ToInt32(await cmd2.ExecuteScalarAsync());

            return (sessionCount, contactCount);
        }

        /// <summary>
        /// Get contacts by LastCalledAt date (for daily reports).
        /// Retrieves all contacts where LastCalledAt falls within the specified date (00:00 to 23:59).
        /// </summary>
        public async Task<List<ContactEntity>> GetContactsByLastCalledDateAsync(DateTime date)
        {
            var contacts = new List<ContactEntity>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Get date range for the specified day
            var startDate = date.Date.ToString("O");
            var endDate = date.Date.AddDays(1).AddTicks(-1).ToString("O");

            var sql = @"
                SELECT * FROM Contact 
                WHERE LastCalledAt IS NOT NULL 
                  AND LastCalledAt >= @StartDate 
                  AND LastCalledAt <= @EndDate
                  AND Status != 0
                ORDER BY LastCalledAt DESC
            ";

            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                contacts.Add(new ContactEntity
                {
                    Id = reader.GetInt32(0),
                    SessionKey = reader.GetString(1),
                    OriginalId = reader.GetInt32(2),
                    Name = reader.GetString(3),
                    PhoneNumber = reader.GetString(4),
                    Company = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Status = reader.GetInt32(6),
                    Note = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    LastCalledAt = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }

            return contacts;
        }

        /// <summary>
        /// Update contact status and set LastCalledAt timestamp.
        /// </summary>
        public async Task UpdateContactStatusAsync(int contactId, int status)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE Contact 
                SET Status = @Status, LastCalledAt = @LastCalledAt
                WHERE Id = @Id
            ";

            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", contactId);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@LastCalledAt", DateTime.Now.ToString("O"));
            
            await cmd.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
