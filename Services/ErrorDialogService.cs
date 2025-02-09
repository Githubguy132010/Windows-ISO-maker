using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace Windows_ISO_Maker.Services
{
    public class ErrorDialogService
    {
        public static void ShowError(string title, string message, string? details = null)
        {
            var dialog = new TaskDialog
            {
                Title = title,
                Content = message,
                FooterVisibility = details != null ? Visibility.Visible : Visibility.Collapsed,
                Footer = details,
                Icon = SymbolRegular.ErrorCircle24,
            };

            var closeButton = new TaskDialogButton
            {
                Content = "Close",
                Appearance = ControlAppearance.Secondary
            };

            if (message.Contains("ADK"))
            {
                var downloadButton = new TaskDialogButton
                {
                    Content = "Download ADK",
                    Appearance = ControlAppearance.Primary
                };
                
                downloadButton.Click += (s, e) =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install",
                        UseShellExecute = true
                    });
                    dialog.Hide();
                };

                dialog.Buttons.Add(downloadButton);
            }

            dialog.Buttons.Add(closeButton);
            dialog.Show();
        }
    }
}