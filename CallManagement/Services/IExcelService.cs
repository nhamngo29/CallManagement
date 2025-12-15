using CallManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CallManagement.Services
{
    /// <summary>
    /// Service interface for Excel import/export operations.
    /// </summary>
    public interface IExcelService
    {
        /// <summary>
        /// Import contacts from an Excel file.
        /// </summary>
        /// <param name="filePath">Path to the .xlsx file.</param>
        /// <returns>Import result with contacts and statistics.</returns>
        Task<ImportResult> ImportAsync(string filePath);

        /// <summary>
        /// Export contacts to an Excel file.
        /// </summary>
        /// <param name="filePath">Path to save the .xlsx file.</param>
        /// <param name="contacts">Contacts to export.</param>
        /// <param name="sessionName">Optional session name for metadata.</param>
        Task ExportAsync(string filePath, IEnumerable<Contact> contacts, string? sessionName = null);

        /// <summary>
        /// Generate default export filename based on current timestamp.
        /// </summary>
        string GenerateExportFilename();
    }
}
