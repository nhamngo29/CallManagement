using CallManagement.Models;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Service for sending Excel reports via Telegram Bot API.
    /// Uses HTTP API directly without third-party Telegram libraries.
    /// </summary>
    public class TelegramReportService : ITelegramReportService, IDisposable
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        private const string TELEGRAM_API_BASE = "https://api.telegram.org/bot";
        private const int MAX_RETRIES = 2;
        private static readonly TimeSpan REQUEST_TIMEOUT = TimeSpan.FromSeconds(60);
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

        public TelegramReportService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _httpClient = new HttpClient
            {
                Timeout = REQUEST_TIMEOUT
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CONFIGURATION CHECK
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
        public async Task<string?> ValidateConfigurationAsync()
        {
            if (SettingsService.Instance == null)
                return "Settings service not initialized";

            var settings = await SettingsService.Instance.LoadTelegramSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.BotToken))
                return "Bot Token chưa được cấu hình";

            if (!settings.BotToken.Contains(':'))
                return "Bot Token không hợp lệ";

            if (string.IsNullOrWhiteSpace(settings.ChatId))
                return "Chat ID chưa được cấu hình";

            // Test connection by calling getMe
            try
            {
                var url = $"{TELEGRAM_API_BASE}{settings.BotToken}/getMe";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if (error.Contains("Unauthorized") || error.Contains("401"))
                        return "Bot Token không hợp lệ hoặc đã hết hạn";
                    return $"Lỗi kết nối Telegram: {response.StatusCode}";
                }

                return null; // Valid
            }
            catch (TaskCanceledException)
            {
                return "Timeout kết nối tới Telegram";
            }
            catch (HttpRequestException ex)
            {
                return $"Lỗi mạng: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Lỗi không xác định: {ex.Message}";
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SEND REPORTS
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<List<SendReportResult>> SendExcelReportsAsync(
            IEnumerable<string> sessionKeys,
            Action<SendReportProgress>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<SendReportResult>();
            var sessionKeysList = new List<string>(sessionKeys);
            int total = sessionKeysList.Count;
            int current = 0;

            // Get Telegram settings
            var settings = await SettingsService.Instance.LoadTelegramSettingsAsync();

            foreach (var sessionKey in sessionKeysList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                current++;
                progressCallback?.Invoke(new SendReportProgress
                {
                    Current = current,
                    Total = total,
                    CurrentSessionKey = sessionKey
                });

                var result = await SendSingleReportAsync(sessionKey, settings, cancellationToken);
                results.Add(result);

                // Small delay between requests to avoid rate limiting
                if (current < total)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }

            return results;
        }

        /// <summary>
        /// Send a single session report via Telegram.
        /// </summary>
        private async Task<SendReportResult> SendSingleReportAsync(
            string sessionKey,
            SettingsService.TelegramSettings settings,
            CancellationToken cancellationToken)
        {
            var result = new SendReportResult { SessionKey = sessionKey };

            try
            {
                // 1. Load contacts from database
                var contacts = await _databaseService.GetContactsBySessionAsync(sessionKey);
                
                if (contacts == null || contacts.Count == 0)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Session không có dữ liệu";
                    return result;
                }

                // 2. Generate Excel file in memory
                using var stream = new MemoryStream();
                await ExportToExcelStreamAsync(contacts, sessionKey, stream);
                stream.Position = 0;

                // 3. Send via Telegram with retry
                var fileName = $"CallSession_{sessionKey}.xlsx";
                var sendResult = await SendDocumentWithRetryAsync(
                    settings.BotToken,
                    settings.ChatId,
                    stream,
                    fileName,
                    cancellationToken);

                result.IsSuccess = sendResult.IsSuccess;
                result.ErrorMessage = sendResult.ErrorMessage;
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Đã hủy";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Export contacts to an Excel stream.
        /// </summary>
        private async Task ExportToExcelStreamAsync(
            List<ContactEntity> contacts,
            string sessionKey,
            MemoryStream stream)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Contacts");

                // Headers
                var headers = new[] { "Tên", "SĐT", "Công ty", "Ghi chú", "Trạng thái" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Data rows
                int row = 2;
                foreach (var contact in contacts)
                {
                    var status = (CallStatus)contact.Status;

                    worksheet.Cell(row, 1).Value = contact.Name;
                    worksheet.Cell(row, 2).Value = contact.PhoneNumber;
                    worksheet.Cell(row, 3).Value = contact.Company;
                    worksheet.Cell(row, 4).Value = contact.Note;
                    worksheet.Cell(row, 5).Value = GetStatusText(status);

                    // Style status cell
                    var statusCell = worksheet.Cell(row, 5);
                    var (bgColor, textColor) = GetStatusColors(status);
                    statusCell.Style.Fill.BackgroundColor = bgColor;
                    statusCell.Style.Font.FontColor = textColor;
                    statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Borders
                    for (int col = 1; col <= 5; col++)
                    {
                        worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(row, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E5E7EB");
                    }

                    row++;
                }

                // Formatting
                worksheet.Column(4).Style.Alignment.WrapText = true;
                worksheet.Column(4).Width = 40;
                worksheet.Column(1).AdjustToContents(1, row, 15, 30);
                worksheet.Column(2).AdjustToContents(1, row, 12, 20);
                worksheet.Column(3).AdjustToContents(1, row, 15, 30);
                worksheet.Column(5).AdjustToContents(1, row, 12, 20);
                worksheet.SheetView.FreezeRows(1);

                if (row > 2)
                {
                    worksheet.RangeUsed()?.SetAutoFilter();
                }

                workbook.SaveAs(stream);
            });
        }

        /// <summary>
        /// Send document to Telegram with retry logic.
        /// </summary>
        private async Task<(bool IsSuccess, string? ErrorMessage)> SendDocumentWithRetryAsync(
            string botToken,
            string chatId,
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < MAX_RETRIES)
            {
                attempt++;
                
                try
                {
                    // Reset stream position for retry
                    fileStream.Position = 0;

                    var url = $"{TELEGRAM_API_BASE}{botToken}/sendDocument";

                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(chatId), "chat_id");
                    
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    content.Add(fileContent, "document", fileName);

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
                    lastException = new Exception("Timeout gửi file");
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
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        private static string GetStatusText(CallStatus status)
        {
            return status switch
            {
                CallStatus.Interested => "Có nhu cầu",
                CallStatus.NotInterested => "Không nhu cầu",
                CallStatus.NoAnswer => "Không bắt máy",
                CallStatus.Busy => "Máy bận",
                CallStatus.InvalidNumber => "Số không tồn tại",
                _ => "Chưa gọi"
            };
        }

        private static (XLColor Background, XLColor Text) GetStatusColors(CallStatus status)
        {
            return status switch
            {
                CallStatus.Interested => (XLColor.FromHtml("#DCFCE7"), XLColor.FromHtml("#166534")),
                CallStatus.NotInterested => (XLColor.FromHtml("#FEE2E2"), XLColor.FromHtml("#991B1B")),
                CallStatus.NoAnswer => (XLColor.FromHtml("#F3F4F6"), XLColor.FromHtml("#374151")),
                CallStatus.Busy => (XLColor.FromHtml("#FEF3C7"), XLColor.FromHtml("#92400E")),
                CallStatus.InvalidNumber => (XLColor.FromHtml("#7F1D1D"), XLColor.White),
                _ => (XLColor.White, XLColor.FromHtml("#6B7280"))
            };
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
