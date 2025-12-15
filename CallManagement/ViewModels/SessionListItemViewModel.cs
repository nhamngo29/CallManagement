using CallManagement.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CallManagement.ViewModels
{
    /// <summary>
    /// ViewModel for each session item in the sidebar list.
    /// Provides display properties and selection state for sidebar navigation.
    /// </summary>
    public partial class SessionListItemViewModel : ViewModelBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Session key (null for current session).
        /// </summary>
        public string? SessionKey { get; }

        /// <summary>
        /// Whether this is the current editable session.
        /// </summary>
        public bool IsCurrent { get; }

        /// <summary>
        /// Display time text (e.g., "15/12/25 14:30").
        /// </summary>
        public string DisplayTime { get; }

        /// <summary>
        /// Tooltip with full information.
        /// </summary>
        public string ToolTipText { get; }

        /// <summary>
        /// Icon for the session type.
        /// </summary>
        public string Icon => IsCurrent ? "🟢" : "🕒";

        /// <summary>
        /// Number of contacts in this session.
        /// </summary>
        public int ContactCount { get; }

        /// <summary>
        /// Reference to associated tab (for lazy loading).
        /// </summary>
        public SessionTabViewModel? Tab { get; set; }

        /// <summary>
        /// Whether this item is currently selected.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Created timestamp for sorting.
        /// </summary>
        public DateTime CreatedAt { get; }

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTORS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Create a session list item for current session.
        /// </summary>
        public SessionListItemViewModel()
        {
            IsCurrent = true;
            SessionKey = null;
            DisplayTime = "Current Session";
            ToolTipText = "Phiên làm việc hiện tại - có thể chỉnh sửa";
            ContactCount = 0;
            CreatedAt = DateTime.MaxValue; // Always at top
        }

        /// <summary>
        /// Create a session list item from saved session.
        /// </summary>
        public SessionListItemViewModel(CallSession session)
        {
            IsCurrent = false;
            SessionKey = session.SessionKey;
            ContactCount = session.ContactCount;

            if (DateTime.TryParse(session.CreatedAt, out var date))
            {
                CreatedAt = date;
                DisplayTime = date.ToString("dd/MM/yy HH:mm");
                ToolTipText = $"Saved at {date:dd/MM/yyyy HH:mm:ss}\n{session.ContactCount} contacts";
            }
            else
            {
                CreatedAt = DateTime.Now;
                DisplayTime = session.SessionKey;
                ToolTipText = $"Session: {session.SessionKey}\n{session.ContactCount} contacts";
            }
        }

        /// <summary>
        /// Create from existing tab.
        /// </summary>
        public SessionListItemViewModel(SessionTabViewModel tab)
        {
            Tab = tab;
            IsCurrent = tab.IsCurrentSession;
            SessionKey = tab.SessionKey;
            ContactCount = tab.Contacts.Count;

            if (IsCurrent)
            {
                DisplayTime = "Current Session";
                ToolTipText = "Phiên làm việc hiện tại - có thể chỉnh sửa";
                CreatedAt = DateTime.MaxValue;
            }
            else if (tab.Session != null)
            {
                if (DateTime.TryParse(tab.Session.CreatedAt, out var date))
                {
                    CreatedAt = date;
                    DisplayTime = date.ToString("dd/MM/yy HH:mm");
                    ToolTipText = $"Saved at {date:dd/MM/yyyy HH:mm:ss}\n{ContactCount} contacts";
                }
                else
                {
                    CreatedAt = DateTime.Now;
                    DisplayTime = tab.Session.SessionKey;
                    ToolTipText = $"Session: {tab.Session.SessionKey}";
                }
            }
            else
            {
                CreatedAt = DateTime.Now;
                DisplayTime = "Unknown";
                ToolTipText = "Unknown session";
            }
        }
    }
}
