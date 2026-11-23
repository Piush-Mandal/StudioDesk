namespace StudioDesk.Core;

/// <summary>
/// Where StudioDesk keeps its files.
///   Install: C:\Program Files\PipoZa\StudioDesk\          (read-only, admin-installed)
///   Data:    C:\Program Files\PipoZa\StudioDesk\Data\     (encrypted app resources)
///   User:    %LOCALAPPDATA%\PipoZa\StudioDesk\UserData\   (encrypted, per Windows user)
/// </summary>
public sealed class AppPaths
{
    public string InstallDir { get; }
    public string DataDir { get; }
    public string UserDataDir { get; }
    public string KeysDir => Path.Combine(UserDataDir, "Keys");
    public string SettingsDir => Path.Combine(UserDataDir, "Settings");
    public string LogsDir => Path.Combine(UserDataDir, "Logs");
    public string ContentDir => Path.Combine(UserDataDir, "Content");
    public string CacheDir => Path.Combine(UserDataDir, "Cache");

    public AppPaths(string installDir, string userDataDir)
    {
        InstallDir = installDir;
        DataDir = Path.Combine(installDir, "Data");
        UserDataDir = userDataDir;
    }

    public static AppPaths Default() => new(
        AppContext.BaseDirectory,
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PipoZa", "StudioDesk", "UserData"));

    public void EnsureUserDirectories()
    {
        foreach (var dir in new[] { UserDataDir, KeysDir, SettingsDir, LogsDir, ContentDir, CacheDir })
            Directory.CreateDirectory(dir);
    }
}
