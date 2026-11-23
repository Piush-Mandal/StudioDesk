using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudioDesk.Core.Storage;

public enum AppTheme { System, Light, Dark }

/// <summary>User settings. Add new fields here; unknown/missing fields fall back to defaults.</summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string Language { get; set; } = "en";
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }

    /// <summary>Custom export folder chosen by the user; null = default (Documents\StudioDesk).</summary>
    public string? ExportDirectory { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>Loads/saves settings as an encrypted file in UserData/Settings.</summary>
public sealed class SettingsService(EncryptedStore store)
{
    private const string FilePath = "Settings/settings.json";

    public AppSettings Load()
    {
        var json = store.ReadText(FilePath);
        return json is null ? new AppSettings()
            : JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
    }

    public void Save(AppSettings settings) =>
        store.WriteText(FilePath, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings));
}
