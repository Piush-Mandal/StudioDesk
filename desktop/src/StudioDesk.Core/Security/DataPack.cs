using System.IO.Compression;
using System.Text;

namespace StudioDesk.Core.Security;

/// <summary>
/// Encrypted resource pack in Program Files\PipoZa\StudioDesk\Data (*.sdpak).
/// Plaintext is a ZIP archive; the whole archive (including file names) is AES-256-GCM
/// encrypted and bound to the pack name. Decrypted only in memory, never to disk.
/// </summary>
public sealed class DataPack : IDisposable
{
    private static readonly byte[] Magic = "SDPK"u8.ToArray();
    public const string Extension = ".sdpak";
    private readonly ZipArchive _zip;

    private DataPack(ZipArchive zip) => _zip = zip;

    public IEnumerable<string> Entries => _zip.Entries.Select(e => e.FullName);

    public static DataPack Open(string packPath, ReadOnlySpan<byte> dataKey)
    {
        var name = Path.GetFileNameWithoutExtension(packPath);
        var plain = AesGcmCipher.Decrypt(dataKey, Magic, Context(name), File.ReadAllBytes(packPath));
        return new DataPack(new ZipArchive(new MemoryStream(plain, writable: false), ZipArchiveMode.Read));
    }

    public byte[] ReadBytes(string entryName)
    {
        var entry = _zip.GetEntry(entryName) ?? throw new FileNotFoundException($"'{entryName}' is not in this pack.");
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public string ReadText(string entryName) => Encoding.UTF8.GetString(ReadBytes(entryName));

    /// <summary>Used by the build-time packer: encrypts a folder into a pack.</summary>
    public static void Create(string sourceDir, string packPath, ReadOnlySpan<byte> dataKey)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var rel = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                var entry = zip.CreateEntry(rel, CompressionLevel.SmallestSize);
                entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero); // hide build times
                using var es = entry.Open();
                using var fs = File.OpenRead(file);
                fs.CopyTo(es);
            }
        }
        var name = Path.GetFileNameWithoutExtension(packPath);
        AtomicFile.Write(packPath, AesGcmCipher.Encrypt(dataKey, Magic, Context(name), ms.ToArray()));
    }

    private static byte[] Context(string packName) => Encoding.UTF8.GetBytes("pack:" + packName.ToLowerInvariant());

    public void Dispose() => _zip.Dispose();
}
