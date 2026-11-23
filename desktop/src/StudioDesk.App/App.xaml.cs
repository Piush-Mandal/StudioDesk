using System.Security.Cryptography;
using Microsoft.UI.Xaml;

namespace StudioDesk.App;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => Services?.Log.Error("Unhandled exception", e.Exception);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Services = AppServices.Start();
        }
        catch (Exception ex) when (ex is CryptographicException or FileNotFoundException or DirectoryNotFoundException)
        {
            AppServices.ShowFatalError(
                "StudioDesk's files are missing or have been modified.\n\nPlease reinstall StudioDesk.");
            Exit();
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
