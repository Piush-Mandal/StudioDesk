using System.Runtime.InteropServices;
using System.Text.Json;
using StudioDesk.App.Generated;
using StudioDesk.Core;
using StudioDesk.Core.Security;
using StudioDesk.Core.Storage;

namespace StudioDesk.App;

/// <summary>Everything the app needs at runtime: paths, encrypted storage, settings, log, data packs.</summary>
public sealed partial class AppServices : IDisposable
{
    private readonly MasterKey _masterKey;
    private readonly DataPack _corePack;

    public AppPaths Paths { get; }
    public EncryptedStore Store { get; }
    public SettingsService SettingsService { get; }
    public AppSettings Settings { get; }
    public EncryptedLog Log { get; }
    public string Tagline { get; }
    public ExportLocation Export { get; } = ExportLocation.ForCurrentUser();

    /// <summary>The folder exports go to right now (user's choice, or Documents\StudioDesk).</summary>
    public string ResolveExportDirectory(out bool usedFallback) =>
        Export.Resolve(Settings.ExportDirectory, out usedFallback);

    private AppServices(AppPaths paths)
    {
        Paths = paths;
        Paths.EnsureUserDirectories();
        _masterKey = MasterKey.LoadOrCreate(paths.KeysDir, new DpapiKeyProtector());
        Store = new EncryptedStore(paths.UserDataDir, _masterKey);
        Log = new EncryptedLog(Store, paths.LogsDir);
        SettingsService = new SettingsService(Store);
        Settings = SettingsService.Load();

        var dataKey = EmbeddedKeys.DataKey();
        try { _corePack = DataPack.Open(Path.Combine(paths.DataDir, "core" + DataPack.Extension), dataKey); }
        finally { Array.Clear(dataKey); }

        using var appInfo = JsonDocument.Parse(_corePack.ReadBytes("app.json"));
        Tagline = appInfo.RootElement.GetProperty("tagline").GetString() ?? "";
        Log.Info("StudioDesk started");
    }

    public static AppServices Start() => new(AppPaths.Default());

    public void SaveSettings() => SettingsService.Save(Settings);

    public void Dispose()
    {
        _corePack.Dispose();
        _masterKey.Dispose();
    }

    /// <summary>Shown when the app cannot start (e.g. Data files missing or tampered with).</summary>
    public static void ShowFatalError(string message) =>
        MessageBoxW(IntPtr.Zero, message, "StudioDesk", 0x10 /* MB_ICONERROR */);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
