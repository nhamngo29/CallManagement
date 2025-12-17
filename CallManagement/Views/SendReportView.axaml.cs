using Avalonia.Controls;
using Avalonia.Input;
using CallManagement.ViewModels;

namespace CallManagement.Views
{
    public partial class SendReportView : UserControl
    {
        public SendReportView()
        {
            InitializeComponent();
            
            // Handle keyboard shortcuts
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is SendReportViewModel viewModel)
            {
                // ESC to close/cancel
                if (e.Key == Key.Escape)
                {
                    viewModel.CancelCommand.Execute(null);
                    e.Handled = true;
                }
                // Enter to send (when not sending and has selections)
                else if (e.Key == Key.Enter && !viewModel.IsSending && viewModel.HasSelectedSessions)
                {
                    viewModel.SendCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
