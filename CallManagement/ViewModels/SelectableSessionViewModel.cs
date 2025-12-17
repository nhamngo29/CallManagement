using CommunityToolkit.Mvvm.ComponentModel;

namespace CallManagement.ViewModels
{
    /// <summary>
    /// ViewModel for a selectable session item in the Send Report popup.
    /// </summary>
    public partial class SelectableSessionViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private string _sessionKey = string.Empty;

        [ObservableProperty]
        private string _displayText = string.Empty;

        [ObservableProperty]
        private int _contactCount;

        [ObservableProperty]
        private string _createdAt = string.Empty;

        public SelectableSessionViewModel()
        {
        }

        public SelectableSessionViewModel(string sessionKey, string displayText, int contactCount, string createdAt)
        {
            _sessionKey = sessionKey;
            _displayText = displayText;
            _contactCount = contactCount;
            _createdAt = createdAt;
        }
    }
}
