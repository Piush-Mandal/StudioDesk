using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StudioDesk.App.Pages;
using StudioDesk.Core.Storage;

namespace StudioDesk.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var s = App.Services.Settings;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            Math.Clamp(s.WindowWidth ?? 1200, 800, 4000),
            Math.Clamp(s.WindowHeight ?? 760, 560, 3000)));
        ApplyTheme(s.Theme);

        Nav.SelectedItem = Nav.MenuItems[0];
        Closed += OnClosed;
    }

    public nint Handle => WinRT.Interop.WindowNative.GetWindowHandle(this);

    public void ApplyTheme(AppTheme theme) => Root.RequestedTheme = theme switch
    {
        AppTheme.Light => ElementTheme.Light,
        AppTheme.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected) ContentFrame.Navigate(typeof(SettingsPage));
        else if (args.SelectedItem is NavigationViewItem { Tag: "home" }) ContentFrame.Navigate(typeof(HomePage));
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        var size = AppWindow.Size;
        App.Services.Settings.WindowWidth = size.Width;
        App.Services.Settings.WindowHeight = size.Height;
        App.Services.SaveSettings();
        App.Services.Log.Info("StudioDesk closed");
        App.Services.Dispose();
    }
}
