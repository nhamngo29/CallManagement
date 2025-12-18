using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CallManagement.Models;
using CallManagement.ViewModels;
using System.Runtime.InteropServices;

namespace CallManagement.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize ViewModel asynchronously after window is loaded
            Loaded += OnWindowLoaded;
            
            // Add tunneling event handler for key events (fires before bubbling)
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        }

        private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                // Set MainWindow reference for file dialogs
                viewModel.MainWindow = this;
                
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// Handle double-tap on row to enter edit mode.
        /// </summary>
        private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Border border && border.DataContext is Contact contact)
            {
                contact.EnterEditModeCommand.Execute(null);
            }
        }

        /// <summary>
        /// Preview key handler - tunnel event that fires before the TextBox handles Enter.
        /// This allows us to intercept Cmd+Enter/Ctrl+Enter before AcceptsReturn processes it.
        /// Also handles global keyboard shortcuts.
        /// </summary>
        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // Check for global keyboard shortcuts first
            if (DataContext is MainWindowViewModel viewModel)
            {
                bool isModifierPressed = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? e.KeyModifiers.HasFlag(KeyModifiers.Meta)  // Cmd on macOS
                    : e.KeyModifiers.HasFlag(KeyModifiers.Control);  // Ctrl on Windows/Linux

                // Ctrl/Cmd + I = Import Excel
                if (e.Key == Key.I && isModifierPressed)
                {
                    viewModel.ImportExcelCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Ctrl/Cmd + E = Export Excel
                if (e.Key == Key.E && isModifierPressed)
                {
                    viewModel.ExportExcelCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Ctrl/Cmd + S = Save Session
                if (e.Key == Key.S && isModifierPressed)
                {
                    viewModel.SaveSessionCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }

            // Handle note textarea specific shortcuts
            if (e.Source is TextBox textBox && textBox.Classes.Contains("noteTextArea"))
            {
                if (textBox.DataContext is Contact contact)
                {
                    // Check for Ctrl+Enter (Windows/Linux) or Cmd+Enter (macOS)
                    bool isModifierPressed = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? e.KeyModifiers.HasFlag(KeyModifiers.Meta)  // Cmd on macOS
                        : e.KeyModifiers.HasFlag(KeyModifiers.Control);  // Ctrl on Windows/Linux

                    if (e.Key == Key.Enter && isModifierPressed)
                    {
                        contact.SaveEditCommand.Execute(null);
                        e.Handled = true;
                        return;
                    }
                    
                    // Escape to cancel
                    if (e.Key == Key.Escape)
                    {
                        contact.CancelEditCommand.Execute(null);
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Handle keyboard shortcuts in note textarea (bubbling event - backup handler).
        /// </summary>
        private void OnNoteKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            
            if (sender is TextBox textBox && textBox.DataContext is Contact contact)
            {
                // Escape to cancel (in case tunnel didn't catch it)
                if (e.Key == Key.Escape)
                {
                    contact.CancelEditCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handle click on settings overlay background to close settings panel.
        /// </summary>
        private void SettingsOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.CloseSettingsCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handle click on send report overlay background to close popup.
        /// </summary>
        private void SendReportOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.CloseSendReportCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handle click on daily report preview overlay background to close popup.
        /// </summary>
        private void DailyReportPreviewOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.CloseDailyReportPreviewCommand.Execute(null);
            }
        }
    }
}