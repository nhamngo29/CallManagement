using CallManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Service for generating and sending daily call reports via Telegram.
    /// Reports are based on LastCalledAt timestamp, not session.
    /// </summary>
    public class DailyReportService : IDailyReportService, IDisposable
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        private const string TELEGRAM_API_BASE = "https://api.telegram.org/bot";
        private const int MAX_MESSAGE_LENGTH = 4096; // Telegram message limit
        private const int MAX_RETRIES = 2;
        private static readonly TimeSpan REQUEST_TIMEOUT = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromSeconds(2);

        // ═══════════════════════════════════════════════════════════════════════
        // FIELDS
        // ═══════════════════════════════════════════════════════════════════════

        private readonly HttpClient _httpClient;
        private readonly DatabaseService _databaseService;
        private bool _disposed;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════

        public DailyReportService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _httpClient = new HttpClient
            {
                Timeout = REQUEST_TIMEOUT
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<bool> IsConfiguredAsync()
        {
            if (SettingsService.Instance == null)
                return false;

            var settings = await SettingsService.Instance.LoadTelegramSettingsAsync();
            return !string.IsNullOrWhiteSpace(settings.BotToken) &&
                   !string.IsNullOrWhiteSpace(settings.ChatId) &&
                   settings.BotToken.Contains(':');
        }

        /// <inheritdoc/>
        public async Task<DailyReportResult> SendDailyReportAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            var result = new DailyReportResult();

            try
            {
                // 1. Validate configuration
                if (!await IsConfiguredAsync())
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Telegram chưa được cấu hình";
                    return result;
                }

                // 2. Get contacts called today
                var contacts = await GetContactsCalledOnDateAsync(date);

                if (contacts.Count == 0)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Không có cuộc gọi nào trong ngày {date:dd/MM/yyyy}";
                    return result;
                }

                // 3. Build markdown report
                var reportParts = BuildMarkdownReport(date, contacts);

                // 4. Send via Telegram
                var settings = await SettingsService.Instance.LoadTelegramSettingsAsync();

                foreach (var part in reportParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var sendResult = await SendMessageWithRetryAsync(settings.BotToken, settings.ChatId, part, cancellationToken);
                    
                    if (!sendResult.IsSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = sendResult.ErrorMessage;
                        return result;
                    }

                    // Small delay between messages to avoid rate limiting
                    if (reportParts.IndexOf(part) < reportParts.Count - 1)
                    {
                        await Task.Delay(300, cancellationToken);
                    }
                }

                // 5. Set result statistics
                result.IsSuccess = true;
                result.TotalCalls = contacts.Count;
                result.InterestedCount = contacts.Count(c => c.Status == (int)CallStatus.Interested);
                result.NotInterestedCount = contacts.Count(c => c.Status == (int)CallStatus.NotInterested);
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Đã hủy";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<string> GetReportPreviewAsync(DateTime date)
        {
            var contacts = await GetContactsCalledOnDateAsync(date);
            var reportParts = BuildMarkdownReport(date, contacts);
            return string.Join("\n\n---\n\n", reportParts);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - DATA RETRIEVAL
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Get all contacts that were called on the specified date.
        /// Uses LastCalledAt field from database.
        /// </summary>
        private async Task<List<ContactEntity>> GetContactsCalledOnDateAsync(DateTime date)
        {
            return await _databaseService.GetContactsByLastCalledDateAsync(date);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - REPORT GENERATION
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Build markdown report content, split into multiple messages if needed.
        /// </summary>
        private List<string> BuildMarkdownReport(DateTime date, List<ContactEntity> contacts)
        {
            var parts = new List<string>();

            // Calculate statistics
            var interestedContacts = contacts.Where(c => c.Status == (int)CallStatus.Interested).ToList();
            var notInterestedContacts = contacts.Where(c => c.Status == (int)CallStatus.NotInterested).ToList();
            var noAnswerContacts = contacts.Where(c => c.Status == (int)CallStatus.NoAnswer).ToList();
            var busyContacts = contacts.Where(c => c.Status == (int)CallStatus.Busy).ToList();
            var invalidContacts = contacts.Where(c => c.Status == (int)CallStatus.InvalidNumber).ToList();

            // Part 1: Header and Overview
            var sb = new StringBuilder();
            sb.AppendLine("📊 *Daily Call Report*");
            sb.AppendLine($"🗓 Date: {date:dd/MM/yyyy}");
            sb.AppendLine();
            sb.AppendLine("*📞 Tổng quan*");
            sb.AppendLine($"• Tổng số cuộc gọi: *{contacts.Count}*");
            sb.AppendLine($"• Có nhu cầu: *{interestedContacts.Count}*");
            sb.AppendLine($"• Không nhu cầu: *{notInterestedContacts.Count}*");
            sb.AppendLine($"• Không bắt máy: *{noAnswerContacts.Count}*");
            sb.AppendLine($"• Máy bận: *{busyContacts.Count}*");
            sb.AppendLine($"• Số không tồn tại: *{invalidContacts.Count}*");

            parts.Add(sb.ToString());

            // Part 2: Interested customers (split if too long)
            if (interestedContacts.Count > 0)
            {
                var interestedParts = BuildCustomerList("⭐ Khách hàng có nhu cầu", interestedContacts);
                parts.AddRange(interestedParts);
            }

            // Part 3: Not interested customers (split if too long)
            if (notInterestedContacts.Count > 0)
            {
                var notInterestedParts = BuildCustomerList("❌ Khách hàng không nhu cầu", notInterestedContacts);
                parts.AddRange(notInterestedParts);
            }

            // Part 4: Summary
            sb = new StringBuilder();
            sb.AppendLine("*📌 Tổng kết*");
            sb.AppendLine($"• Tổng khách hàng có nhu cầu: *{interestedContacts.Count}*");
            sb.AppendLine($"• Tổng khách hàng không nhu cầu: *{notInterestedContacts.Count}*");

            parts.Add(sb.ToString());

            return parts;
        }

        /// <summary>
        /// Build customer list section, split into multiple parts if too long.
        /// </summary>
        private List<string> BuildCustomerList(string header, List<ContactEntity> contacts)
        {
            var parts = new List<string>();
            var sb = new StringBuilder();
            sb.AppendLine($"*{header} ({contacts.Count})*");

            int index = 1;
            foreach (var contact in contacts)
            {
                var line = $"{index}. {EscapeMarkdown(contact.Name)} – {contact.PhoneNumber}";
                
                // Check if adding this line would exceed limit
                if (sb.Length + line.Length + 2 > MAX_MESSAGE_LENGTH - 100) // Leave some buffer
                {
                    parts.Add(sb.ToString());
                    sb = new StringBuilder();
                    sb.AppendLine($"*{header} (tiếp theo)*");
                }

                sb.AppendLine(line);
                index++;
            }

            if (sb.Length > 0)
            {
                parts.Add(sb.ToString());
            }

            return parts;
        }

        /// <summary>
        /// Escape special markdown characters for Telegram.
        /// </summary>
        private string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Escape special characters in Markdown V1: _ * ` [
            return text
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("`", "\\`")
                .Replace("[", "\\[");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - TELEGRAM API
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Send message to Telegram with retry logic.
        /// </summary>
        private async Task<(bool IsSuccess, string? ErrorMessage)> SendMessageWithRetryAsync(
            string botToken,
            string chatId,
            string message,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < MAX_RETRIES)
            {
                attempt++;

                try
                {
                    var url = $"{TELEGRAM_API_BASE}{botToken}/sendMessage";

                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("chat_id", chatId),
                        new KeyValuePair<string, string>("text", message),
                        new KeyValuePair<string, string>("parse_mode", "Markdown")
                    });

                    var response = await _httpClient.PostAsync(url, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return (true, null);
                    }

                    // Parse error
                    if (responseBody.Contains("chat not found") || responseBody.Contains("400"))
                    {
                        return (false, "Chat ID không hợp lệ");
                    }
                    if (responseBody.Contains("Unauthorized") || responseBody.Contains("401"))
                    {
                        return (false, "Bot Token không hợp lệ");
                    }
                    if (responseBody.Contains("429"))
                    {
                        // Rate limited, wait and retry
                        await Task.Delay(RETRY_DELAY * attempt, cancellationToken);
                        continue;
                    }

                    lastException = new Exception($"HTTP {response.StatusCode}: {responseBody}");
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new Exception("Timeout gửi message");
                }
                catch (HttpRequestException ex)
                {
                    lastException = new Exception($"Lỗi mạng: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                if (attempt < MAX_RETRIES)
                {
                    await Task.Delay(RETRY_DELAY, cancellationToken);
                }
            }

            return (false, lastException?.Message ?? "Lỗi không xác định sau nhiều lần thử");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed) return;
            _httpClient.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
