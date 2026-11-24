using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StudioDesk.Core.Storage;
using Windows.Storage.Pickers;

namespace StudioDesk.App.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();
        var services = App.Services;
        ThemeChoice.SelectedIndex = (int)services.Settings.Theme;
        UserDataPath.Text = "Stored in: " + services.Paths.UserDataDir;
        VersionText.Text = "StudioDesk " + (typeof(App).Assembly.GetName().Version?.ToString(3) ?? "");
        RefreshExport();
        _loading = false;
    }

    private void ThemeChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThemeChoice.SelectedIndex < 0) return;
        var theme = (AppTheme)ThemeChoice.SelectedIndex;
        App.Services.Settings.Theme = theme;
        App.Services.SaveSettings();
        App.MainWindow?.ApplyTheme(theme);
    }

    private void RefreshExport()
    {
        var dir = App.Services.ResolveExportDirectory(out var usedFallback);
        ExportPath.Text = dir;
        ResetExport.IsEnabled = App.Services.Settings.ExportDirectory is not null;
        if (usedFallback)
            Show(InfoBarSeverity.Warning, "Your chosen folder isn't available right now, so exports go to the default folder.");
    }

    private async void ChangeExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainWindow!.Handle);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null) return; // cancelled

            if (ExportLocation.Validate(folder.Path) is { } error)
            {
                Show(InfoBarSeverity.Error, error);
                return;
            }
            App.Services.Settings.ExportDirectory = folder.Path;
            App.Services.SaveSettings();
            ExportInfo.IsOpen = false;
            RefreshExport();
            Show(InfoBarSeverity.Success, "Export folder updated.");
        }
        catch (Exception ex)
        {
            App.Services.Log.Error("Change export folder failed", ex);
            Show(InfoBarSeverity.Error, "Couldn't change the export folder. Please try again.");
        }
    }

    private void ResetExport_Click(object sender, RoutedEventArgs e)
    {
        App.Services.Settings.ExportDirectory = null;
        App.Services.SaveSettings();
        ExportInfo.IsOpen = false;
        RefreshExport();
        Show(InfoBarSeverity.Success, "Export folder reset to default.");
    }

    private void OpenExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = App.Services.ResolveExportDirectory(out _);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            App.Services.Log.Error("Open export folder failed", ex);
            Show(InfoBarSeverity.Error, "Couldn't open the folder.");
        }
    }

    private void Show(InfoBarSeverity severity, string message)
    {
        ExportInfo.Severity = severity;
        ExportInfo.Message = message;
        ExportInfo.IsOpen = true;
    }
}
