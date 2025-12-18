namespace CallManagement.Models
{
    /// <summary>
    /// Columns available for sorting in the contact list.
    /// </summary>
    public enum SortColumn
    {
        /// <summary>
        /// No sorting applied (default order).
        /// </summary>
        None = 0,

        /// <summary>
        /// Sort by contact name.
        /// </summary>
        Name = 1,

        /// <summary>
        /// Sort by phone number.
        /// </summary>
        PhoneNumber = 2,

        /// <summary>
        /// Sort by company name.
        /// </summary>
        Company = 3,

        /// <summary>
        /// Sort by call status.
        /// </summary>
        Status = 4
    }

    /// <summary>
    /// Sort direction for contact list.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// No sorting (default order).
        /// </summary>
        None = 0,

        /// <summary>
        /// Ascending order (A-Z, 0-9).
        /// </summary>
        Ascending = 1,

        /// <summary>
        /// Descending order (Z-A, 9-0).
        /// </summary>
        Descending = 2
    }
}
