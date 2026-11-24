namespace StudioDesk.Core.Storage;

/// <summary>
/// Where exported files go. Exports are normal (unencrypted) files the user owns.
///
/// Default: [Documents]\StudioDesk for the current Windows user. Documents is resolved with
/// the Windows Known Folder API, so it is always correct for any username and follows
/// OneDrive folder backup automatically, e.g.
///   OneDrive on:  C:\Users\[name]\OneDrive\Documents\StudioDesk
///   OneDrive off: C:\Users\[name]\Documents\StudioDesk
///   Redirected (company PC / moved Documents): wherever Windows says Documents is.
/// The user can pick any other folder in Settings; if it becomes unavailable
/// (deleted, USB drive removed, no permission) we fall back to the default.
/// </summary>
public sealed class ExportLocation(Func<string> documentsFolder, Func<string> userProfileFolder)
{
    public const string FolderName = "StudioDesk";

    public static ExportLocation ForCurrentUser() => new(
        () => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify),
        () => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>The default export folder for this user (not created).</summary>
    public string DefaultDirectory
    {
        get
        {
            var docs = documentsFolder();
            if (string.IsNullOrWhiteSpace(docs))
                docs = Path.Combine(userProfileFolder(), "Documents");
            return Path.Combine(docs, FolderName);
        }
    }

    /// <summary>
    /// Returns the folder to export into and creates it if needed.
    /// <paramref name="usedFallback"/> is true when the user's custom folder could not be used.
    /// </summary>
    public string Resolve(string? customDirectory, out bool usedFallback)
    {
        usedFallback = false;
        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            if (TryPrepare(customDirectory, out var custom)) return custom;
            usedFallback = true;
        }
        if (TryPrepare(DefaultDirectory, out var def)) return def;
        throw new IOException($"Cannot create the export folder '{DefaultDirectory}'.");
    }

    /// <summary>Validates a folder the user picked in Settings. Returns an error message, or null if OK.</summary>
    public static string? Validate(string directory) =>
        TryPrepare(directory, out _) ? null : "StudioDesk can't save files in this folder. Choose another one.";

    private static bool TryPrepare(string directory, out string fullPath)
    {
        fullPath = "";
        try
        {
            if (!Path.IsPathFullyQualified(directory)) return false;
            fullPath = Path.GetFullPath(directory);
            Directory.CreateDirectory(fullPath);
            // Confirm we can actually write here (read-only folders, OneDrive offline, permissions)
            var probe = Path.Combine(fullPath, $".sd-write-test-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Returns a free file path in the folder: "Name.ext", then "Name (2).ext", ... never overwrites.</summary>
    public static string UniqueFilePath(string directory, string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
        var path = Path.Combine(directory, fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 2; File.Exists(path); i++)
            path = Path.Combine(directory, $"{name} ({i}){ext}");
        return path;
    }
}
