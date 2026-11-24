using System.Text;
using StudioDesk.Core.Security;

namespace StudioDesk.Core.Storage;

/// <summary>
/// Reads and writes files under UserData. Every file is AES-256-GCM encrypted and
/// bound to its own relative path, so files cannot be read, edited or swapped outside StudioDesk.
/// </summary>
public sealed class EncryptedStore
{
    private static readonly byte[] Magic = "SDUD"u8.ToArray();
    private readonly string _root;
    private readonly byte[] _key;

    public EncryptedStore(string userDataDir, MasterKey masterKey)
    {
        _root = Path.GetFullPath(userDataDir);
        _key = masterKey.DeriveSubkey("UserData.Files");
    }

    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath, out _));

    public void WriteBytes(string relativePath, ReadOnlySpan<byte> plaintext)
    {
        var path = Resolve(relativePath, out var context);
        AtomicFile.Write(path, AesGcmCipher.Encrypt(_key, Magic, context, plaintext));
    }

    public byte[]? ReadBytes(string relativePath)
    {
        var path = Resolve(relativePath, out var context);
        return File.Exists(path) ? AesGcmCipher.Decrypt(_key, Magic, context, File.ReadAllBytes(path)) : null;
    }

    public void WriteText(string relativePath, string text) => WriteBytes(relativePath, Encoding.UTF8.GetBytes(text));

    public string? ReadText(string relativePath) =>
        ReadBytes(relativePath) is { } bytes ? Encoding.UTF8.GetString(bytes) : null;

    public void Delete(string relativePath)
    {
        var path = Resolve(relativePath, out _);
        if (File.Exists(path)) File.Delete(path);
    }

    private string Resolve(string relativePath, out byte[] context)
    {
        var normalized = relativePath.Replace('\\', '/').Trim('/');
        var full = Path.GetFullPath(Path.Combine(_root, normalized));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path escapes UserData.");
        context = Encoding.UTF8.GetBytes(normalized.ToLowerInvariant());
        return full + ".sdx";
    }
}
