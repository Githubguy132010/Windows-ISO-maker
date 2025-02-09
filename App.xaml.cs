using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace Windows_ISO_Maker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize theme listener for dynamic theme changes
            var windowsThemeManager = new WindowsThemeManager();
            windowsThemeManager.ThemeChanged += (s, ev) =>
            {
                if (ev.NewTheme == WindowsTheme.Dark)
                    Theme.Apply(ThemeType.Dark);
                else
                    Theme.Apply(ThemeType.Light);
            };
        }
    }
}