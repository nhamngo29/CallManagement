namespace CallManagement.Models
{
    /// <summary>
    /// Contact entity for database storage.
    /// This is the database model, separate from the UI Contact model.
    /// </summary>
    public class ContactEntity
    {
        /// <summary>
        /// Primary key, auto-increment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to CallSession.SessionKey.
        /// </summary>
        public string SessionKey { get; set; } = string.Empty;

        /// <summary>
        /// Original ID from imported data (preserves order).
        /// </summary>
        public int OriginalId { get; set; }

        /// <summary>
        /// Contact name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Company name.
        /// </summary>
        public string Company { get; set; } = string.Empty;

        /// <summary>
        /// Call status (stored as integer).
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Note/comment about the call.
        /// </summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// ISO datetime string of last call attempt.
        /// </summary>
        public string? LastCalledAt { get; set; }

        /// <summary>
        /// Convert to UI Contact model.
        /// </summary>
        public Contact ToContact()
        {
            return new Contact
            {
                Id = OriginalId,
                Name = Name,
                PhoneNumber = PhoneNumber,
                Company = Company,
                Status = (CallStatus)Status,
                Note = Note,
                IsActionsEnabled = Status == (int)CallStatus.None
            };
        }

        /// <summary>
        /// Create from UI Contact model.
        /// </summary>
        public static ContactEntity FromContact(Contact contact, string sessionKey)
        {
            return new ContactEntity
            {
                SessionKey = sessionKey,
                OriginalId = contact.Id,
                Name = contact.Name,
                PhoneNumber = contact.PhoneNumber,
                Company = contact.Company,
                Status = (int)contact.Status,
                Note = contact.Note,
                LastCalledAt = null
            };
        }
    }
}
