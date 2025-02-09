using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wpf.Ui.Controls;
using Windows_ISO_Maker.Services;

namespace Windows_ISO_Maker
{
    public partial class MainWindow : FluentWindow
    {
        private string? selectedIsoPath;
        private string? driversPath;
        private readonly IsoService _isoService;
        private readonly PrerequisiteService _prerequisiteService;

        public MainWindow()
        {
            InitializeComponent();
            _prerequisiteService = new PrerequisiteService();
            _prerequisiteService.DownloadProgressChanged += PrerequisiteService_DownloadProgressChanged;
            _isoService = new IsoService(_prerequisiteService);
            _isoService.ProgressChanged += IsoService_ProgressChanged;
            
            Loaded += MainWindow_Loaded;
        }

        private void PrerequisiteService_DownloadProgressChanged(object? sender, ToolsDownloadProgressEventArgs e)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => PrerequisiteService_DownloadProgressChanged(sender, e));
                return;
            }

            StatusText.Text = e.Status;
            
            if (e.TotalBytes.HasValue && e.TotalBytes > 0)
            {
                double percentage = (double)e.BytesTransferred / e.TotalBytes.Value * 100;
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = percentage;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressRing.Visibility = Visibility.Collapsed;
            }
            else
            {
                ProgressBar.IsIndeterminate = true;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressRing.Visibility = Visibility.Visible;
            }

            // Update download component info if we have it
            if (!string.IsNullOrEmpty(e.Component))
            {
                DownloadComponentText.Text = e.Component;
                DownloadComponentText.Visibility = Visibility.Visible;
            }
            else
            {
                DownloadComponentText.Visibility = Visibility.Collapsed;
            }
        }

        private void IsoService_ProgressChanged(object? sender, IsoProgressEventArgs e)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => IsoService_ProgressChanged(sender, e));
                return;
            }

            StatusText.Text = e.Status;
            
            if (e.IsIndeterminate)
            {
                ProgressBar.IsIndeterminate = true;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressRing.Visibility = Visibility.Visible;
            }
            else if (e.ProgressPercentage.HasValue)
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = e.ProgressPercentage.Value;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressRing.Visibility = Visibility.Collapsed;
            }
            else
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressRing.Visibility = Visibility.Collapsed;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_prerequisiteService.ArePrerequisitesInstalled())
            {
                MainContent.IsEnabled = false;
                StatusText.Text = "Setting up required tools...";
                ProgressRing.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;

                try
                {
                    await _prerequisiteService.ExtractBundledTools();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("ADK"))
                    {
                        ErrorDialogService.ShowError(
                            "Windows ADK Required",
                            "Windows Assessment and Deployment Kit (ADK) is required to create custom Windows ISOs.",
                            "Click 'Download ADK' to open the Microsoft download page.\n\nAfter installing ADK, restart the application.");
                    }
                    else
                    {
                        ErrorDialogService.ShowError(
                            "Tool Setup Failed",
                            "Failed to set up required tools",
                            ex.Message);
                    }
                }
                finally
                {
                    MainContent.IsEnabled = true;
                    StatusText.Text = "Ready";
                    ProgressRing.Visibility = Visibility.Collapsed;
                    ProgressBar.Visibility = Visibility.Collapsed;
                    DownloadComponentText.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SelectIsoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Windows ISO File",
                Filters = { new CommonFileDialogFilter("ISO Files", "*.iso") },
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                selectedIsoPath = dialog.FileName;
                ShowSnackbar("ISO selected: " + Path.GetFileName(selectedIsoPath));
            }
        }

        private void AddDriversButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Drivers Folder",
                IsFolderPicker = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                driversPath = dialog.FileName;
                ShowSnackbar("Drivers folder selected: " + Path.GetFileName(driversPath));
            }
        }

        private async void CreateIsoButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedIsoPath))
            {
                ShowSnackbar("Please select a Windows ISO first", ControlAppearance.Danger);
                return;
            }

            CreateIsoButton.IsEnabled = false;
            SelectIsoButton.IsEnabled = false;
            AddDriversButton.IsEnabled = false;

            try
            {
                bool enableWinRE = EnableWinRECheckBox.IsChecked ?? false;
                bool keepOriginalOptions = KeepOriginalOptionsCheckBox.IsChecked ?? false;

                var (success, message) = await _isoService.CustomizeIso(
                    selectedIsoPath,
                    driversPath,
                    enableWinRE,
                    keepOriginalOptions);

                if (success)
                {
                    ShowSnackbar(message, ControlAppearance.Success);
                }
                else
                {
                    if (message.Contains("ADK"))
                    {
                        ErrorDialogService.ShowError(
                            "Windows ADK Required",
                            "Windows Assessment and Deployment Kit (ADK) is required to create custom Windows ISOs.",
                            "Click 'Download ADK' to open the Microsoft download page.\n\nAfter installing ADK, restart the application.");
                    }
                    else
                    {
                        ErrorDialogService.ShowError(
                            "ISO Creation Failed",
                            message);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialogService.ShowError(
                    "Error",
                    "An unexpected error occurred",
                    ex.Message);
            }
            finally
            {
                CreateIsoButton.IsEnabled = true;
                SelectIsoButton.IsEnabled = true;
                AddDriversButton.IsEnabled = true;
                StatusText.Text = "Ready";
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressRing.Visibility = Visibility.Collapsed;
                DownloadComponentText.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowSnackbar(string message, ControlAppearance appearance = ControlAppearance.Info)
        {
            var snackbar = new Snackbar
            {
                Title = message,
                Appearance = appearance,
                Timeout = 5000
            };

            snackbar.Show();
        }
    }
}