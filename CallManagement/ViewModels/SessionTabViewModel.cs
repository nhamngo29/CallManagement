using CallManagement.Models;
using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CallManagement.ViewModels
{
    /// <summary>
    /// ViewModel for each tab representing a call session.
    /// Can be either the current editable session or a read-only history session.
    /// </summary>
    public partial class SessionTabViewModel : ViewModelBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Session key (null for current session, set for history).
        /// </summary>
        [ObservableProperty]
        private string? _sessionKey;

        /// <summary>
        /// Display title for the tab header.
        /// </summary>
        [ObservableProperty]
        private string _title = "Current Session";

        /// <summary>
        /// Tooltip text showing full datetime.
        /// </summary>
        [ObservableProperty]
        private string _toolTip = "Phiên làm việc hiện tại";

        /// <summary>
        /// Whether this is the current editable session.
        /// </summary>
        [ObservableProperty]
        private bool _isCurrentSession = true;

        /// <summary>
        /// Whether this tab is read-only (history tabs).
        /// </summary>
        public bool IsReadOnly => !IsCurrentSession;

        /// <summary>
        /// Icon for the tab header.
        /// </summary>
        public string Icon => IsCurrentSession ? "📋" : "🕒";

        /// <summary>
        /// Collection of contacts in this session.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Contact> _contacts = new();

        /// <summary>
        /// Currently selected contact.
        /// </summary>
        [ObservableProperty]
        private Contact? _selectedContact;

        /// <summary>
        /// Currently editing contact (only one at a time).
        /// </summary>
        [ObservableProperty]
        private Contact? _currentlyEditingContact;

        /// <summary>
        /// Whether the session is loading data.
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Whether data has been loaded.
        /// </summary>
        [ObservableProperty]
        private bool _isLoaded;

        /// <summary>
        /// Associated CallSession model (for history tabs).
        /// </summary>
        public CallSession? Session { get; set; }

        // ═══════════════════════════════════════════════════════════════════════
        // STATISTICS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _answeredCount;

        [ObservableProperty]
        private int _noAnswerCount;

        [ObservableProperty]
        private int _invalidCount;

        [ObservableProperty]
        private int _busyCount;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTORS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Create a new current session tab.
        /// </summary>
        public SessionTabViewModel()
        {
            IsCurrentSession = true;
            Title = "📋 Phiên hiện tại";
            ToolTip = "Danh sách đang làm việc - có thể chỉnh sửa";
            IsLoaded = true;
        }

        /// <summary>
        /// Create a history session tab from saved session.
        /// </summary>
        public SessionTabViewModel(CallSession session)
        {
            Session = session;
            SessionKey = session.SessionKey;
            IsCurrentSession = false;
            Title = $"🕒 {session.FormattedDate}";
            ToolTip = $"Saved at {session.FormattedDate} ({session.ContactCount} contacts)";
            IsLoaded = false; // Will load on demand
        }

        // ═══════════════════════════════════════════════════════════════════════
        // LOAD DATA
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Load contacts from database for history session.
        /// </summary>
        public async Task LoadContactsAsync()
        {
            if (IsLoaded || string.IsNullOrEmpty(SessionKey)) return;

            IsLoading = true;
            try
            {
                var db = DatabaseService.Instance;
                var entities = await db.GetContactsBySessionAsync(SessionKey);

                Contacts.Clear();
                foreach (var entity in entities)
                {
                    var contact = entity.ToContact();
                    // History contacts are read-only
                    contact.IsActionsEnabled = false;
                    SubscribeToContactEvents(contact);
                    Contacts.Add(contact);
                }

                UpdateStatistics();
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load contacts: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CONTACT MANAGEMENT
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Add a contact to this session.
        /// </summary>
        public void AddContact(Contact contact)
        {
            SubscribeToContactEvents(contact);
            Contacts.Add(contact);
            UpdateStatistics();
        }

        /// <summary>
        /// Set contacts for this session (replacing existing).
        /// </summary>
        public void SetContacts(ObservableCollection<Contact> contacts)
        {
            // Unsubscribe from old contacts
            foreach (var contact in Contacts)
            {
                UnsubscribeFromContactEvents(contact);
            }

            Contacts = contacts;

            // Subscribe to new contacts
            foreach (var contact in Contacts)
            {
                SubscribeToContactEvents(contact);
            }

            UpdateStatistics();
        }

        /// <summary>
        /// Clone all contacts (for saving to new session).
        /// </summary>
        public ObservableCollection<Contact> CloneContacts()
        {
            var cloned = new ObservableCollection<Contact>();
            foreach (var contact in Contacts)
            {
                cloned.Add(new Contact(
                    contact.Id,
                    contact.Name,
                    contact.PhoneNumber,
                    contact.Company,
                    contact.Note
                )
                {
                    Status = contact.Status,
                    IsActionsEnabled = contact.IsActionsEnabled
                });
            }
            return cloned;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CONTACT EVENT HANDLING
        // ═══════════════════════════════════════════════════════════════════════

        private void SubscribeToContactEvents(Contact contact)
        {
            contact.EditModeEntered += OnContactEditModeEntered;
            contact.EditModeExited += OnContactEditModeExited;
        }

        private void UnsubscribeFromContactEvents(Contact contact)
        {
            contact.EditModeEntered -= OnContactEditModeEntered;
            contact.EditModeExited -= OnContactEditModeExited;
        }

        private void OnContactEditModeEntered(Contact contact)
        {
            if (IsReadOnly) return; // Don't allow editing in history tabs

            if (CurrentlyEditingContact != null && CurrentlyEditingContact != contact)
            {
                CurrentlyEditingContact.ForceExitEditMode();
            }
            CurrentlyEditingContact = contact;
        }

        private void OnContactEditModeExited(Contact contact)
        {
            if (CurrentlyEditingContact == contact)
            {
                CurrentlyEditingContact = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STATISTICS
        // ═══════════════════════════════════════════════════════════════════════

        public void UpdateStatistics()
        {
            TotalCount = Contacts.Count;
            AnsweredCount = Contacts.Count(c => c.Status == CallStatus.Answered);
            NoAnswerCount = Contacts.Count(c => c.Status == CallStatus.NoAnswer);
            InvalidCount = Contacts.Count(c => c.Status == CallStatus.InvalidNumber);
            BusyCount = Contacts.Count(c => c.Status == CallStatus.Busy);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CALL STATUS COMMANDS (only for current session)
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void SetAnswered(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.Answered);
        }

        [RelayCommand]
        private void SetNoAnswer(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.NoAnswer);
        }

        [RelayCommand]
        private void SetInvalidNumber(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.InvalidNumber);
        }

        [RelayCommand]
        private void SetBusy(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.Busy);
        }

        [RelayCommand]
        private void ResetStatus(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            contact.Status = CallStatus.None;
            contact.IsActionsEnabled = true;
            UpdateStatistics();
        }

        private void SetContactStatus(Contact contact, CallStatus status)
        {
            contact.Status = status;
            contact.IsActionsEnabled = false;
            UpdateStatistics();
        }
    }
}
