using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace StudioDesk.Core.Security;

/// <summary>Wraps/unwraps the user master key so it is never stored in plain form.</summary>
public interface IKeyProtector
{
    byte[] Protect(byte[] key);
    byte[] Unprotect(byte[] protectedKey);
}

/// <summary>
/// Windows DPAPI, CurrentUser scope: only this Windows account on this PC can unwrap the key.
/// Copying UserData to another PC or account leaves it unreadable.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiKeyProtector : IKeyProtector
{
    private static readonly byte[] Entropy = "PipoZa.StudioDesk.UserMasterKey.v1"u8.ToArray();

    public byte[] Protect(byte[] key) =>
        ProtectedData.Protect(key, Entropy, DataProtectionScope.CurrentUser);

    public byte[] Unprotect(byte[] protectedKey) =>
        ProtectedData.Unprotect(protectedKey, Entropy, DataProtectionScope.CurrentUser);
}
