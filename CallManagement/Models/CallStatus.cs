namespace CallManagement.Models
{
    /// <summary>
    /// Represents the status of a call attempt.
    /// </summary>
    public enum CallStatus
    {
        /// <summary>
        /// No status has been set yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The call was not answered.
        /// </summary>
        NoAnswer = 1,

        /// <summary>
        /// The line was busy.
        /// </summary>
        Busy = 2,

        /// <summary>
        /// The phone number is invalid/does not exist.
        /// </summary>
        InvalidNumber = 3,

        /// <summary>
        /// Customer is interested (has demand).
        /// </summary>
        Interested = 4,

        /// <summary>
        /// Customer is not interested (no demand).
        /// </summary>
        NotInterested = 5
    }
}
