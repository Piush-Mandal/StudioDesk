using System.Security.Cryptography;

namespace StudioDesk.Core.Security;

/// <summary>
/// The per-user master key (random 256-bit). Stored on disk only in DPAPI-wrapped form.
/// Subkeys for each purpose are derived with HKDF so one key never encrypts two kinds of data.
/// </summary>
public sealed class MasterKey : IDisposable
{
    private const string FileName = "master.key";
    private readonly byte[] _key;

    private MasterKey(byte[] key) => _key = key;

    public static MasterKey LoadOrCreate(string keysDir, IKeyProtector protector)
    {
        Directory.CreateDirectory(keysDir);
        var path = Path.Combine(keysDir, FileName);
        if (File.Exists(path))
            return new MasterKey(protector.Unprotect(File.ReadAllBytes(path)));

        var key = RandomNumberGenerator.GetBytes(AesGcmCipher.KeySize);
        AtomicFile.Write(path, protector.Protect(key));
        return new MasterKey(key);
    }

    public byte[] DeriveSubkey(string purpose) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, _key, AesGcmCipher.KeySize,
            salt: "StudioDesk.v1"u8.ToArray(),
            info: System.Text.Encoding.UTF8.GetBytes(purpose));

    public void Dispose() => CryptographicOperations.ZeroMemory(_key);
}
