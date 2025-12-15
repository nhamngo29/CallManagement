using CallManagement.Models;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Excel import/export service using ClosedXML.
    /// Cross-platform compatible (Windows & macOS).
    /// </summary>
    public class ExcelService : IExcelService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // COLUMN HEADER MAPPINGS (Case-insensitive, Vietnamese support)
        // ═══════════════════════════════════════════════════════════════════════

        private static readonly string[] NameHeaders = 
        { 
            "tên", "ten", "name", "họ tên", "ho ten", "họ và tên", "ho va ten", 
            "full name", "fullname", "customer name", "khách hàng", "khach hang" 
        };

        private static readonly string[] PhoneHeaders = 
        { 
            "sđt", "sdt", "số điện thoại", "so dien thoai", "điện thoại", "dien thoai",
            "phone", "phone number", "phonenumber", "mobile", "tel", "telephone",
            "số dt", "so dt", "dt"
        };

        private static readonly string[] CompanyHeaders = 
        { 
            "công ty", "cong ty", "company", "doanh nghiệp", "doanh nghiep",
            "tổ chức", "to chuc", "organization", "org", "cty"
        };

        private static readonly string[] NoteHeaders = 
        { 
            "ghi chú", "ghi chu", "note", "notes", "mô tả", "mo ta", 
            "description", "comment", "comments", "nhận xét", "nhan xet"
        };

        // Phone validation regex: only digits, 9-11 characters
        private static readonly Regex PhoneRegex = new(@"^\d{9,11}$", RegexOptions.Compiled);

        // ═══════════════════════════════════════════════════════════════════════
        // IMPORT
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<ImportResult> ImportAsync(string filePath)
        {
            return await Task.Run(() => ImportInternal(filePath));
        }

        private ImportResult ImportInternal(string filePath)
        {
            try
            {
                // Validate file exists
                if (!File.Exists(filePath))
                {
                    return new ImportResult
                    {
                        ErrorMessage = "File không tồn tại"
                    };
                }

                // Validate file extension
                if (!filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    return new ImportResult
                    {
                        ErrorMessage = "Chỉ hỗ trợ file .xlsx"
                    };
                }

                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return new ImportResult
                    {
                        ErrorMessage = "File Excel không có sheet nào"
                    };
                }

                // Find header row and column mappings
                var columnMap = FindColumnMappings(worksheet);

                if (!columnMap.ContainsKey("Name"))
                {
                    return new ImportResult
                    {
                        ErrorMessage = "Không tìm thấy cột 'Tên' trong file"
                    };
                }

                if (!columnMap.ContainsKey("Phone"))
                {
                    return new ImportResult
                    {
                        ErrorMessage = "Không tìm thấy cột 'SĐT' trong file"
                    };
                }

                // Process rows
                var contacts = new List<Contact>();
                var skipReasons = new Dictionary<string, int>();
                int totalRows = 0;
                int skippedCount = 0;
                int contactId = 1;

                // Get used range
                var usedRange = worksheet.RangeUsed();
                if (usedRange == null)
                {
                    return new ImportResult
                    {
                        ErrorMessage = "File Excel trống"
                    };
                }

                int headerRow = columnMap["HeaderRow"];
                int lastRow = usedRange.LastRow().RowNumber();

                for (int row = headerRow + 1; row <= lastRow; row++)
                {
                    totalRows++;

                    try
                    {
                        var result = ProcessRow(worksheet, row, columnMap, contactId);

                        if (result.Contact != null)
                        {
                            contacts.Add(result.Contact);
                            contactId++;
                        }
                        else if (!string.IsNullOrEmpty(result.SkipReason))
                        {
                            skippedCount++;
                            if (!skipReasons.ContainsKey(result.SkipReason))
                            {
                                skipReasons[result.SkipReason] = 0;
                            }
                            skipReasons[result.SkipReason]++;
                        }
                    }
                    catch
                    {
                        skippedCount++;
                        const string reason = "lỗi đọc dữ liệu";
                        if (!skipReasons.ContainsKey(reason))
                        {
                            skipReasons[reason] = 0;
                        }
                        skipReasons[reason]++;
                    }
                }

                return new ImportResult
                {
                    Contacts = contacts,
                    SkippedCount = skippedCount,
                    TotalRows = totalRows,
                    SkipReasons = skipReasons
                };
            }
            catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("access"))
            {
                return new ImportResult
                {
                    ErrorMessage = "File đang được mở bởi ứng dụng khác. Vui lòng đóng file và thử lại."
                };
            }
            catch (Exception ex)
            {
                return new ImportResult
                {
                    ErrorMessage = $"Lỗi đọc file: {ex.Message}"
                };
            }
        }

        private Dictionary<string, int> FindColumnMappings(IXLWorksheet worksheet)
        {
            var columnMap = new Dictionary<string, int>();
            var usedRange = worksheet.RangeUsed();

            if (usedRange == null) return columnMap;

            // Search first 10 rows for header
            int lastCol = usedRange.LastColumn().ColumnNumber();

            for (int row = 1; row <= Math.Min(10, usedRange.LastRow().RowNumber()); row++)
            {
                bool foundName = false;
                bool foundPhone = false;

                for (int col = 1; col <= lastCol; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString().Trim().ToLowerInvariant();
                    cellValue = RemoveVietnameseDiacritics(cellValue);

                    if (!columnMap.ContainsKey("Name") && NameHeaders.Any(h => 
                        cellValue == RemoveVietnameseDiacritics(h) || cellValue.Contains(RemoveVietnameseDiacritics(h))))
                    {
                        columnMap["Name"] = col;
                        foundName = true;
                    }

                    if (!columnMap.ContainsKey("Phone") && PhoneHeaders.Any(h => 
                        cellValue == RemoveVietnameseDiacritics(h) || cellValue.Contains(RemoveVietnameseDiacritics(h))))
                    {
                        columnMap["Phone"] = col;
                        foundPhone = true;
                    }

                    if (!columnMap.ContainsKey("Company") && CompanyHeaders.Any(h => 
                        cellValue == RemoveVietnameseDiacritics(h) || cellValue.Contains(RemoveVietnameseDiacritics(h))))
                    {
                        columnMap["Company"] = col;
                    }

                    if (!columnMap.ContainsKey("Note") && NoteHeaders.Any(h => 
                        cellValue == RemoveVietnameseDiacritics(h) || cellValue.Contains(RemoveVietnameseDiacritics(h))))
                    {
                        columnMap["Note"] = col;
                    }
                }

                // Found required columns in this row
                if (foundName && foundPhone)
                {
                    columnMap["HeaderRow"] = row;
                    break;
                }

                // Reset if not all required found
                if (!foundName || !foundPhone)
                {
                    columnMap.Remove("Name");
                    columnMap.Remove("Phone");
                    columnMap.Remove("Company");
                    columnMap.Remove("Note");
                }
            }

            return columnMap;
        }

        private (Contact? Contact, string? SkipReason) ProcessRow(
            IXLWorksheet worksheet, 
            int row, 
            Dictionary<string, int> columnMap,
            int contactId)
        {
            // Get cell values
            var name = worksheet.Cell(row, columnMap["Name"]).GetString().Trim();
            var phone = worksheet.Cell(row, columnMap["Phone"]).GetString().Trim();
            
            var company = columnMap.ContainsKey("Company") 
                ? worksheet.Cell(row, columnMap["Company"]).GetString().Trim() 
                : string.Empty;
            
            var note = columnMap.ContainsKey("Note") 
                ? worksheet.Cell(row, columnMap["Note"]).GetString().Trim() 
                : string.Empty;

            // Skip empty rows
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone))
            {
                return (null, null); // Silent skip for empty rows
            }

            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                return (null, "thiếu tên");
            }

            // Validate phone
            if (string.IsNullOrWhiteSpace(phone))
            {
                return (null, "thiếu SĐT");
            }

            // Clean phone number (remove spaces, dashes, dots)
            var cleanPhone = Regex.Replace(phone, @"[\s\-\.\(\)\+]", "");
            
            // Remove leading 0 if starts with 84 (Vietnam country code)
            if (cleanPhone.StartsWith("84") && cleanPhone.Length > 10)
            {
                cleanPhone = "0" + cleanPhone.Substring(2);
            }
            
            // Remove leading + or 00
            if (cleanPhone.StartsWith("+"))
            {
                cleanPhone = cleanPhone.Substring(1);
            }
            if (cleanPhone.StartsWith("00"))
            {
                cleanPhone = cleanPhone.Substring(2);
            }

            // Validate phone format
            if (!Regex.IsMatch(cleanPhone, @"^\d+$"))
            {
                return (null, "SĐT không hợp lệ");
            }

            if (cleanPhone.Length < 9 || cleanPhone.Length > 11)
            {
                return (null, "SĐT không đúng độ dài");
            }

            return (new Contact(contactId, name, cleanPhone, company, note), null);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // EXPORT
        // ═══════════════════════════════════════════════════════════════════════

        public async Task ExportAsync(string filePath, IEnumerable<Contact> contacts, string? sessionName = null)
        {
            await Task.Run(() => ExportInternal(filePath, contacts, sessionName));
        }

        private void ExportInternal(string filePath, IEnumerable<Contact> contacts, string? sessionName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sessionName ?? "Call Results");

            // ─────────────────────────────────────────────────────────────────────
            // HEADER ROW
            // ─────────────────────────────────────────────────────────────────────
            var headers = new[] { "Tên", "Số điện thoại", "Công ty", "Ghi chú", "Trạng thái" };
            
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

            // ─────────────────────────────────────────────────────────────────────
            // DATA ROWS
            // ─────────────────────────────────────────────────────────────────────
            int row = 2;
            foreach (var contact in contacts)
            {
                worksheet.Cell(row, 1).Value = contact.Name;
                worksheet.Cell(row, 2).Value = contact.PhoneNumber;
                worksheet.Cell(row, 3).Value = contact.Company;
                worksheet.Cell(row, 4).Value = contact.Note;
                worksheet.Cell(row, 5).Value = GetStatusText(contact.Status);

                // Style status cell based on status
                var statusCell = worksheet.Cell(row, 5);
                var (bgColor, textColor) = GetStatusColors(contact.Status);
                statusCell.Style.Fill.BackgroundColor = bgColor;
                statusCell.Style.Font.FontColor = textColor;
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add borders
                for (int col = 1; col <= 5; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(row, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E5E7EB");
                }

                row++;
            }

            // ─────────────────────────────────────────────────────────────────────
            // FORMATTING
            // ─────────────────────────────────────────────────────────────────────
            
            // Wrap text for Note column
            worksheet.Column(4).Style.Alignment.WrapText = true;
            worksheet.Column(4).Width = 40;

            // Auto-fit other columns
            worksheet.Column(1).AdjustToContents(1, row, 15, 30);
            worksheet.Column(2).AdjustToContents(1, row, 12, 20);
            worksheet.Column(3).AdjustToContents(1, row, 15, 30);
            worksheet.Column(5).AdjustToContents(1, row, 12, 20);

            // Freeze header row
            worksheet.SheetView.FreezeRows(1);

            // Add filter
            if (row > 2)
            {
                worksheet.RangeUsed()?.SetAutoFilter();
            }

            // Save
            workbook.SaveAs(filePath);
        }

        private static string GetStatusText(CallStatus status)
        {
            return status switch
            {
                CallStatus.Answered => "Nghe máy",
                CallStatus.NoAnswer => "Không nghe",
                CallStatus.InvalidNumber => "Số không tồn tại",
                CallStatus.Busy => "Máy bận",
                _ => "Chưa gọi"
            };
        }

        private static (XLColor Background, XLColor Text) GetStatusColors(CallStatus status)
        {
            return status switch
            {
                CallStatus.Answered => (XLColor.FromHtml("#DCFCE7"), XLColor.FromHtml("#166534")),
                CallStatus.NoAnswer => (XLColor.FromHtml("#F3F4F6"), XLColor.FromHtml("#374151")),
                CallStatus.InvalidNumber => (XLColor.FromHtml("#FEE2E2"), XLColor.FromHtml("#991B1B")),
                CallStatus.Busy => (XLColor.FromHtml("#FEF3C7"), XLColor.FromHtml("#92400E")),
                _ => (XLColor.White, XLColor.FromHtml("#6B7280"))
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UTILITIES
        // ═══════════════════════════════════════════════════════════════════════

        public string GenerateExportFilename()
        {
            return $"CallSession_{DateTime.Now:ddMMyyHHmm}.xlsx";
        }

        /// <summary>
        /// Remove Vietnamese diacritics for header matching.
        /// </summary>
        private static string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }
    }
}
