using CommunityToolkit.Mvvm.ComponentModel;

namespace CallManagement.Models
{
    /// <summary>
    /// Represents a contact for calling purposes.
    /// </summary>
    public partial class Contact : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _company = string.Empty;

        [ObservableProperty]
        private CallStatus _status = CallStatus.None;

        [ObservableProperty]
        private bool _isActionsEnabled = true;

        /// <summary>
        /// Creates a new contact instance.
        /// </summary>
        public Contact() { }

        /// <summary>
        /// Creates a new contact with the specified details.
        /// </summary>
        public Contact(int id, string name, string phoneNumber, string company)
        {
            Id = id;
            Name = name;
            PhoneNumber = phoneNumber;
            Company = company;
            Status = CallStatus.None;
            IsActionsEnabled = true;
        }
    }
}
