using CallManagement.Models;
using CallManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
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
        /// All contacts in this session (source of truth).
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Contact> _allContacts = new();

        /// <summary>
        /// Filtered and sorted contacts for display.
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
        // SEARCH / FILTER / SORT PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Search text for filtering contacts.
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Selected status filter. Null means "All".
        /// </summary>
        [ObservableProperty]
        private StatusFilterOption? _selectedStatusFilter;

        /// <summary>
        /// Current sort column.
        /// </summary>
        [ObservableProperty]
        private SortColumn _currentSortColumn = SortColumn.None;

        /// <summary>
        /// Current sort direction.
        /// </summary>
        [ObservableProperty]
        private SortDirection _currentSortDirection = SortDirection.None;

        /// <summary>
        /// Available status filter options.
        /// </summary>
        public StatusFilterOption[] StatusFilterOptions => StatusFilterOption.AllOptions;

        /// <summary>
        /// Whether there are no matching results after filter/search.
        /// </summary>
        public bool HasNoResults => Contacts.Count == 0 && AllContacts.Count > 0;

        /// <summary>
        /// Message to display when no results found.
        /// </summary>
        public string NoResultsMessage => "Không tìm thấy kết quả phù hợp";

        /// <summary>
        /// Debounce timer for search.
        /// </summary>
        private CancellationTokenSource? _searchDebounceToken;

        /// <summary>
        /// Debounce delay in milliseconds.
        /// </summary>
        private const int SearchDebounceMs = 300;

        // ═══════════════════════════════════════════════════════════════════════
        // STATISTICS
        // ═══════════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _interestedCount;

        [ObservableProperty]
        private int _notInterestedCount;

        [ObservableProperty]
        private int _noAnswerCount;

        [ObservableProperty]
        private int _busyCount;

        [ObservableProperty]
        private int _invalidCount;

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
            SelectedStatusFilter = StatusFilterOptions[0]; // "All" by default
            
            // Subscribe to AllContacts changes
            AllContacts.CollectionChanged += OnAllContactsChanged;
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
            SelectedStatusFilter = StatusFilterOptions[0]; // "All" by default
            
            // Subscribe to AllContacts changes
            AllContacts.CollectionChanged += OnAllContactsChanged;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SEARCH / FILTER / SORT CHANGE HANDLERS
        // ═══════════════════════════════════════════════════════════════════════

        partial void OnSearchTextChanged(string value)
        {
            // Cancel previous debounce
            _searchDebounceToken?.Cancel();
            _searchDebounceToken = new CancellationTokenSource();

            // Debounce search
            Task.Delay(SearchDebounceMs, _searchDebounceToken.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(ApplyFilterSortSearch);
                    }
                });
        }

        partial void OnSelectedStatusFilterChanged(StatusFilterOption? value)
        {
            ApplyFilterSortSearch();
        }

        partial void OnCurrentSortColumnChanged(SortColumn value)
        {
            ApplyFilterSortSearch();
            NotifySortStateChanged();
        }

        partial void OnCurrentSortDirectionChanged(SortDirection value)
        {
            ApplyFilterSortSearch();
            NotifySortStateChanged();
        }

        private void OnAllContactsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // When source data changes, reapply filter/sort
            ApplyFilterSortSearch();
            UpdateStatistics();
        }

        private void NotifySortStateChanged()
        {
            OnPropertyChanged(nameof(NameSortIcon));
            OnPropertyChanged(nameof(PhoneSortIcon));
            OnPropertyChanged(nameof(CompanySortIcon));
            OnPropertyChanged(nameof(StatusSortIcon));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SORT ICONS (for column headers)
        // ═══════════════════════════════════════════════════════════════════════

        public string NameSortIcon => GetSortIcon(SortColumn.Name);
        public string PhoneSortIcon => GetSortIcon(SortColumn.PhoneNumber);
        public string CompanySortIcon => GetSortIcon(SortColumn.Company);
        public string StatusSortIcon => GetSortIcon(SortColumn.Status);

        private string GetSortIcon(SortColumn column)
        {
            if (CurrentSortColumn != column)
                return ""; // No icon when not sorting this column
            
            return CurrentSortDirection switch
            {
                SortDirection.Ascending => "▲",
                SortDirection.Descending => "▼",
                _ => ""
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SORT COMMANDS
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void SortByName() => ToggleSort(SortColumn.Name);

        [RelayCommand]
        private void SortByPhone() => ToggleSort(SortColumn.PhoneNumber);

        [RelayCommand]
        private void SortByCompany() => ToggleSort(SortColumn.Company);

        [RelayCommand]
        private void SortByStatus() => ToggleSort(SortColumn.Status);

        private void ToggleSort(SortColumn column)
        {
            if (CurrentSortColumn != column)
            {
                // New column: start with ascending
                CurrentSortColumn = column;
                CurrentSortDirection = SortDirection.Ascending;
            }
            else
            {
                // Same column: cycle through ASC -> DESC -> None
                CurrentSortDirection = CurrentSortDirection switch
                {
                    SortDirection.Ascending => SortDirection.Descending,
                    SortDirection.Descending => SortDirection.None,
                    _ => SortDirection.Ascending
                };

                if (CurrentSortDirection == SortDirection.None)
                {
                    CurrentSortColumn = SortColumn.None;
                }
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void ClearAllFilters()
        {
            SearchText = string.Empty;
            SelectedStatusFilter = StatusFilterOptions[0]; // "All"
            CurrentSortColumn = SortColumn.None;
            CurrentSortDirection = SortDirection.None;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // FILTER / SORT / SEARCH PIPELINE
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Apply filter, search, and sort to AllContacts and update Contacts.
        /// Pipeline: Filter by Status → Search by text → Sort by column.
        /// </summary>
        private void ApplyFilterSortSearch()
        {
            // Preserve current selection
            var previousSelection = SelectedContact;

            // Step 1: Start with all contacts
            IEnumerable<Contact> result = AllContacts;

            // Step 2: Filter by status
            if (SelectedStatusFilter?.StatusValue != null)
            {
                var filterStatus = SelectedStatusFilter.StatusValue.Value;
                result = result.Where(c => c.Status == filterStatus);
            }

            // Step 3: Search by text (case-insensitive, contains)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                result = result.Where(c =>
                    (!string.IsNullOrEmpty(c.Name) && c.Name.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.PhoneNumber) && c.PhoneNumber.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.Company) && c.Company.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.Note) && c.Note.ToLowerInvariant().Contains(searchLower))
                );
            }

            // Step 4: Sort by column
            if (CurrentSortColumn != SortColumn.None && CurrentSortDirection != SortDirection.None)
            {
                result = CurrentSortColumn switch
                {
                    SortColumn.Name => CurrentSortDirection == SortDirection.Ascending
                        ? result.OrderBy(c => c.Name ?? "")
                        : result.OrderByDescending(c => c.Name ?? ""),
                    SortColumn.PhoneNumber => CurrentSortDirection == SortDirection.Ascending
                        ? result.OrderBy(c => c.PhoneNumber ?? "")
                        : result.OrderByDescending(c => c.PhoneNumber ?? ""),
                    SortColumn.Company => CurrentSortDirection == SortDirection.Ascending
                        ? result.OrderBy(c => c.Company ?? "")
                        : result.OrderByDescending(c => c.Company ?? ""),
                    SortColumn.Status => CurrentSortDirection == SortDirection.Ascending
                        ? result.OrderBy(c => (int)c.Status)
                        : result.OrderByDescending(c => (int)c.Status),
                    _ => result
                };
            }

            // Step 5: Update Contacts collection
            var filteredList = result.ToList();
            
            // Clear and repopulate (efficient for small-medium lists)
            Contacts.Clear();
            foreach (var contact in filteredList)
            {
                Contacts.Add(contact);
            }

            // Restore selection if item still exists
            if (previousSelection != null && Contacts.Contains(previousSelection))
            {
                SelectedContact = previousSelection;
            }
            else
            {
                SelectedContact = Contacts.FirstOrDefault();
            }

            // Notify UI about no results state
            OnPropertyChanged(nameof(HasNoResults));
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

                AllContacts.Clear();
                foreach (var entity in entities)
                {
                    var contact = entity.ToContact();
                    // History contacts are read-only
                    contact.IsActionsEnabled = false;
                    SubscribeToContactEvents(contact);
                    AllContacts.Add(contact);
                }

                // Apply filter/sort/search will update Contacts
                ApplyFilterSortSearch();
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
            AllContacts.Add(contact);
            // ApplyFilterSortSearch is called via OnAllContactsChanged
        }

        /// <summary>
        /// Set contacts for this session (replacing existing).
        /// </summary>
        public void SetContacts(ObservableCollection<Contact> contacts)
        {
            // Unsubscribe from old contacts
            foreach (var contact in AllContacts)
            {
                UnsubscribeFromContactEvents(contact);
            }

            AllContacts.Clear();
            
            // Add new contacts to AllContacts
            foreach (var contact in contacts)
            {
                SubscribeToContactEvents(contact);
                AllContacts.Add(contact);
            }

            // ApplyFilterSortSearch is called via OnAllContactsChanged
        }

        /// <summary>
        /// Clone all contacts (for saving to new session).
        /// </summary>
        public ObservableCollection<Contact> CloneContacts()
        {
            var cloned = new ObservableCollection<Contact>();
            foreach (var contact in AllContacts)
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
                    LastCalledAt = contact.LastCalledAt,
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
            // Statistics are based on AllContacts (not filtered)
            TotalCount = AllContacts.Count;
            InterestedCount = AllContacts.Count(c => c.Status == CallStatus.Interested);
            NotInterestedCount = AllContacts.Count(c => c.Status == CallStatus.NotInterested);
            NoAnswerCount = AllContacts.Count(c => c.Status == CallStatus.NoAnswer);
            BusyCount = AllContacts.Count(c => c.Status == CallStatus.Busy);
            InvalidCount = AllContacts.Count(c => c.Status == CallStatus.InvalidNumber);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CALL STATUS COMMANDS (only for current session)
        // ═══════════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void SetInterested(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.Interested);
        }

        [RelayCommand]
        private void SetNotInterested(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.NotInterested);
        }

        [RelayCommand]
        private void SetNoAnswer(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.NoAnswer);
        }

        [RelayCommand]
        private void SetBusy(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.Busy);
        }

        [RelayCommand]
        private void SetInvalidNumber(Contact? contact)
        {
            if (contact == null || IsReadOnly) return;
            SetContactStatus(contact, CallStatus.InvalidNumber);
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
            contact.LastCalledAt = DateTime.Now;
            contact.IsActionsEnabled = false;
            UpdateStatistics();
            
            // Reapply filter if filtering by status (item might need to be hidden/shown)
            if (SelectedStatusFilter?.StatusValue != null)
            {
                ApplyFilterSortSearch();
            }
        }
    }
}
