using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;

namespace CallManagement.Models
{
    /// <summary>
    /// Represents a contact for calling purposes with inline editing support.
    /// </summary>
    public partial class Contact : ObservableObject
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CORE PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════════════════
        // NOTE PROPERTY
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private string _note = string.Empty;

        /// <summary>
        /// Indicates if the contact has a note.
        /// </summary>
        public bool HasNote => !string.IsNullOrWhiteSpace(Note);

        /// <summary>
        /// Preview of the note (first line, truncated).
        /// </summary>
        public string NotePreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Note)) return string.Empty;
                var firstLine = Note.Split('\n')[0];
                return firstLine.Length > 40 ? firstLine.Substring(0, 40) + "…" : firstLine;
            }
        }

        partial void OnNoteChanged(string value)
        {
            OnPropertyChanged(nameof(HasNote));
            OnPropertyChanged(nameof(NotePreview));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // EDITING STATE PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isNoteExpanded;

        // Editing fields (temporary values while editing)
        [ObservableProperty]
        private string _editName = string.Empty;

        [ObservableProperty]
        private string _editPhoneNumber = string.Empty;

        [ObservableProperty]
        private string _editCompany = string.Empty;

        [ObservableProperty]
        private string _editNote = string.Empty;

        // ═══════════════════════════════════════════════════════════════════════
        // VALIDATION PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private string _phoneNumberError = string.Empty;

        [ObservableProperty]
        private bool _hasPhoneError;

        /// <summary>
        /// Check if current edit state is valid for saving.
        /// </summary>
        [ObservableProperty]
        private bool _canSave = true;

        /// <summary>
        /// Validates phone number format (9-11 digits only).
        /// </summary>
        partial void OnEditPhoneNumberChanged(string value)
        {
            ValidatePhoneNumber(value);
            UpdateCanSave();
        }

        /// <summary>
        /// Update CanSave when EditName changes.
        /// </summary>
        partial void OnEditNameChanged(string value)
        {
            UpdateCanSave();
        }

        private void ValidatePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                PhoneNumberError = "Số điện thoại không được để trống";
                HasPhoneError = true;
                return;
            }

            // Remove spaces and dashes for validation
            var cleanPhone = Regex.Replace(phone, @"[\s\-]", "");

            if (!Regex.IsMatch(cleanPhone, @"^\d+$"))
            {
                PhoneNumberError = "Chỉ được nhập số";
                HasPhoneError = true;
                return;
            }

            if (cleanPhone.Length < 9 || cleanPhone.Length > 11)
            {
                PhoneNumberError = "Số điện thoại phải có 9-11 chữ số";
                HasPhoneError = true;
                return;
            }

            PhoneNumberError = string.Empty;
            HasPhoneError = false;
        }

        /// <summary>
        /// Updates the CanSave property based on validation state.
        /// </summary>
        private void UpdateCanSave()
        {
            CanSave = !HasPhoneError && !string.IsNullOrWhiteSpace(EditName);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // EDIT MODE COMMANDS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Event raised when entering edit mode (for MainWindowViewModel to track).
        /// </summary>
        public event Action<Contact>? EditModeEntered;

        /// <summary>
        /// Event raised when exiting edit mode.
        /// </summary>
        public event Action<Contact>? EditModeExited;

        [RelayCommand]
        private void EnterEditMode()
        {
            if (IsEditing) return;

            // Backup current values to edit fields
            EditName = Name;
            EditPhoneNumber = PhoneNumber;
            EditCompany = Company;
            EditNote = Note;

            // Clear validation errors and set initial CanSave state
            PhoneNumberError = string.Empty;
            HasPhoneError = false;
            UpdateCanSave();

            IsEditing = true;
            EditModeEntered?.Invoke(this);
        }

        [RelayCommand]
        private void SaveEdit()
        {
            if (!CanSave) return;

            // Apply changes
            Name = EditName.Trim();
            PhoneNumber = EditPhoneNumber.Trim();
            Company = EditCompany.Trim();
            Note = EditNote.Trim();

            IsEditing = false;
            IsNoteExpanded = false;
            EditModeExited?.Invoke(this);
        }

        [RelayCommand]
        private void CancelEdit()
        {
            // Discard changes - edit fields will be reset on next enter
            IsEditing = false;
            IsNoteExpanded = false;

            // Clear validation
            PhoneNumberError = string.Empty;
            HasPhoneError = false;

            EditModeExited?.Invoke(this);
        }

        [RelayCommand]
        private void ToggleNote()
        {
            if (!IsEditing)
            {
                // If not in edit mode, enter edit mode first
                EnterEditMode();
            }
            IsNoteExpanded = !IsNoteExpanded;
        }

        /// <summary>
        /// Appends a quick note option to the current note.
        /// </summary>
        [RelayCommand]
        private void AddQuickNote(string quickNote)
        {
            if (string.IsNullOrWhiteSpace(quickNote)) return;
            
            if (string.IsNullOrWhiteSpace(EditNote))
            {
                EditNote = quickNote;
            }
            else
            {
                EditNote = EditNote.TrimEnd() + "\n" + quickNote;
            }
        }

        /// <summary>
        /// Force exit edit mode (called by MainWindowViewModel when another row enters edit).
        /// </summary>
        public void ForceExitEditMode()
        {
            if (!IsEditing) return;
            CancelEdit();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTORS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a new contact instance.
        /// </summary>
        public Contact() { }

        /// <summary>
        /// Creates a new contact with the specified details.
        /// </summary>
        public Contact(int id, string name, string phoneNumber, string company, string note = "")
        {
            Id = id;
            Name = name;
            PhoneNumber = phoneNumber;
            Company = company;
            Note = note;
            Status = CallStatus.None;
            IsActionsEnabled = true;
        }
    }
}
