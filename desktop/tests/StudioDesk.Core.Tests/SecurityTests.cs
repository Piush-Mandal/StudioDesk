using System.Security.Cryptography;
using StudioDesk.Core;
using StudioDesk.Core.Security;
using StudioDesk.Core.Storage;

namespace StudioDesk.Core.Tests;

/// <summary>Stand-in for DPAPI so tests run on any OS. XOR is NOT secure; tests only.</summary>
internal sealed class FakeProtector : IKeyProtector
{
    public byte[] Protect(byte[] key) => key.Select(b => (byte)(b ^ 0x5A)).ToArray();
    public byte[] Unprotect(byte[] p) => p.Select(b => (byte)(b ^ 0x5A)).ToArray();
}

public sealed class SecurityTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sd-tests-" + Guid.NewGuid().ToString("N"));
    private readonly AppPaths _paths;

    public SecurityTests()
    {
        _paths = new AppPaths(Path.Combine(_dir, "install"), Path.Combine(_dir, "UserData"));
        _paths.EnsureUserDirectories();
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private EncryptedStore NewStore()
    {
        var mk = MasterKey.LoadOrCreate(_paths.KeysDir, new FakeProtector());
        return new EncryptedStore(_paths.UserDataDir, mk);
    }

    [Fact]
    public void Cipher_roundtrips_and_detects_tampering()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var blob = AesGcmCipher.Encrypt(key, "TEST"u8, "ctx"u8, "hello"u8);
        Assert.Equal("hello"u8.ToArray(), AesGcmCipher.Decrypt(key, "TEST"u8, "ctx"u8, blob));

        blob[^1] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => AesGcmCipher.Decrypt(key, "TEST"u8, "ctx"u8, blob));
    }

    [Fact]
    public void Cipher_rejects_wrong_context_or_key()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var blob = AesGcmCipher.Encrypt(key, "TEST"u8, "a"u8, "x"u8);
        Assert.ThrowsAny<CryptographicException>(() => AesGcmCipher.Decrypt(key, "TEST"u8, "b"u8, blob));
        Assert.ThrowsAny<CryptographicException>(() => AesGcmCipher.Decrypt(RandomNumberGenerator.GetBytes(32), "TEST"u8, "a"u8, blob));
    }

    [Fact]
    public void Store_files_are_unreadable_on_disk_and_survive_restart()
    {
        NewStore().WriteText("Content/note.txt", "secret note");
        var raw = File.ReadAllBytes(Path.Combine(_paths.UserDataDir, "Content", "note.txt.sdx"));
        Assert.DoesNotContain("secret", System.Text.Encoding.UTF8.GetString(raw));

        Assert.Equal("secret note", NewStore().ReadText("Content/note.txt")); // master key reloaded from disk
    }

    [Fact]
    public void Store_detects_swapped_files()
    {
        var store = NewStore();
        store.WriteText("Content/a.txt", "A");
        store.WriteText("Content/b.txt", "B");
        var dir = Path.Combine(_paths.UserDataDir, "Content");
        File.Copy(Path.Combine(dir, "a.txt.sdx"), Path.Combine(dir, "b.txt.sdx"), overwrite: true);
        Assert.ThrowsAny<CryptographicException>(() => store.ReadText("Content/b.txt"));
    }

    [Fact]
    public void Store_blocks_path_escape()
    {
        Assert.Throws<UnauthorizedAccessException>(() => NewStore().WriteText("../evil.txt", "x"));
    }

    [Fact]
    public void Settings_roundtrip()
    {
        var svc = new SettingsService(NewStore());
        Assert.Equal(AppTheme.System, svc.Load().Theme);
        svc.Save(new AppSettings { Theme = AppTheme.Dark, WindowWidth = 1280 });
        var loaded = svc.Load();
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(1280, loaded.WindowWidth);
    }

    [Fact]
    public void Log_is_encrypted()
    {
        var store = NewStore();
        new EncryptedLog(store, _paths.LogsDir).Info("app started");
        var file = Assert.Single(Directory.GetFiles(_paths.LogsDir, "*.sdx"));
        Assert.DoesNotContain("started", File.ReadAllText(file));
    }

    [Fact]
    public void DataPack_roundtrips_and_hides_file_names()
    {
        var src = Path.Combine(_dir, "assets", "core");
        Directory.CreateDirectory(Path.Combine(src, "ui"));
        File.WriteAllText(Path.Combine(src, "ui", "strings.json"), "{\"hi\":\"Hello\"}");
        var key = RandomNumberGenerator.GetBytes(32);
        var pack = Path.Combine(_paths.DataDir, "core.sdpak");

        DataPack.Create(src, pack, key);
        Assert.DoesNotContain("strings", File.ReadAllText(pack));

        using var opened = DataPack.Open(pack, key);
        Assert.Equal("{\"hi\":\"Hello\"}", opened.ReadText("ui/strings.json"));

        // Renaming a pack breaks it (bound to its name)
        var renamed = Path.Combine(_paths.DataDir, "other.sdpak");
        File.Copy(pack, renamed);
        Assert.ThrowsAny<CryptographicException>(() => DataPack.Open(renamed, key));
    }
}

public sealed class ExportLocationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sd-export-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Fact]
    public void Default_uses_documents_folder_as_windows_reports_it()
    {
        var oneDriveDocs = Path.Combine(_dir, "Users", "Someone", "OneDrive", "Documents");
        var loc = new ExportLocation(() => oneDriveDocs, () => Path.Combine(_dir, "Users", "Someone"));
        Assert.Equal(Path.Combine(oneDriveDocs, "StudioDesk"), loc.DefaultDirectory);
        Assert.Equal(loc.DefaultDirectory, loc.Resolve(null, out var fb));
        Assert.False(fb);
        Assert.True(Directory.Exists(loc.DefaultDirectory));
    }

    [Fact]
    public void Falls_back_to_profile_documents_when_known_folder_missing()
    {
        var profile = Path.Combine(_dir, "Users", "Someone");
        var loc = new ExportLocation(() => "", () => profile);
        Assert.Equal(Path.Combine(profile, "Documents", "StudioDesk"), loc.DefaultDirectory);
    }

    [Fact]
    public void Custom_folder_is_used_and_bad_one_falls_back()
    {
        var loc = new ExportLocation(() => Path.Combine(_dir, "Docs"), () => _dir);
        var custom = Path.Combine(_dir, "MyExports");
        Assert.Equal(custom, loc.Resolve(custom, out var fb1));
        Assert.False(fb1);

        Assert.Equal(loc.DefaultDirectory, loc.Resolve("relative/path", out var fb2));
        Assert.True(fb2);
        Assert.NotNull(ExportLocation.Validate("relative/path"));
    }

    [Fact]
    public void Unique_file_path_never_overwrites()
    {
        Directory.CreateDirectory(_dir);
        var first = ExportLocation.UniqueFilePath(_dir, "Report.pdf");
        File.WriteAllText(first, "x");
        Assert.Equal(Path.Combine(_dir, "Report (2).pdf"), ExportLocation.UniqueFilePath(_dir, "Report.pdf"));
        Assert.Equal(Path.Combine(_dir, "a_b.txt"), ExportLocation.UniqueFilePath(_dir, "a/b.txt"));
    }
}
