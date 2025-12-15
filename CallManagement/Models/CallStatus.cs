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
        /// The call was answered.
        /// </summary>
        Answered = 1,

        /// <summary>
        /// The call was not answered.
        /// </summary>
        NoAnswer = 2,

        /// <summary>
        /// The phone number is invalid.
        /// </summary>
        InvalidNumber = 3,

        /// <summary>
        /// The line was busy.
        /// </summary>
        Busy = 4
    }
}
