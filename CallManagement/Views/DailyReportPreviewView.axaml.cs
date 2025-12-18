using Avalonia.Controls;
using Avalonia.Input;

namespace CallManagement.Views;

public partial class DailyReportPreviewView : UserControl
{
    public DailyReportPreviewView()
    {
        InitializeComponent();
        
        // Set initial focus to Cancel button for safety
        Loaded += (_, _) =>
        {
            CancelButton?.Focus();
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.DailyReportPreviewViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CancelCommand.Execute(null);
                e.Handled = true;
                break;
                
            case Key.Enter:
                if (vm.SendReportCommand.CanExecute(null))
                {
                    vm.SendReportCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }
}
