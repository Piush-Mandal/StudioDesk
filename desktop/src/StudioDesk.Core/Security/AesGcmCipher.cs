using System.Security.Cryptography;

namespace StudioDesk.Core.Security;

/// <summary>
/// AES-256-GCM with a random nonce per message.
/// Layout: [magic 4][version 1][nonce 12][tag 16][ciphertext ...]
/// The header and a caller-supplied context are authenticated, so any change
/// to the file, or moving it to another path/purpose, makes decryption fail.
/// </summary>
public static class AesGcmCipher
{
    public const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const byte Version = 1;
    private const int HeaderSize = 4 + 1 + NonceSize + TagSize;

    public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> magic, ReadOnlySpan<byte> context, ReadOnlySpan<byte> plaintext)
    {
        ValidateKey(key);
        ValidateMagic(magic);
        var output = new byte[HeaderSize + plaintext.Length];
        var span = output.AsSpan();
        magic.CopyTo(span);
        span[4] = Version;
        var nonce = span.Slice(5, NonceSize);
        var tag = span.Slice(5 + NonceSize, TagSize);
        RandomNumberGenerator.Fill(nonce);

        var aad = BuildAad(span[..5], context);
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, span[HeaderSize..], tag, aad);
        return output;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> magic, ReadOnlySpan<byte> context, ReadOnlySpan<byte> blob)
    {
        ValidateKey(key);
        ValidateMagic(magic);
        if (blob.Length < HeaderSize || !blob[..4].SequenceEqual(magic) || blob[4] != Version)
            throw new CryptographicException("Not a StudioDesk encrypted file, or unsupported version.");

        var nonce = blob.Slice(5, NonceSize);
        var tag = blob.Slice(5 + NonceSize, TagSize);
        var ciphertext = blob[HeaderSize..];
        var plaintext = new byte[ciphertext.Length];
        var aad = BuildAad(blob[..5], context);
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, aad); // throws if tampered
        return plaintext;
    }

    private static byte[] BuildAad(ReadOnlySpan<byte> header, ReadOnlySpan<byte> context)
    {
        var aad = new byte[header.Length + context.Length];
        header.CopyTo(aad);
        context.CopyTo(aad.AsSpan(header.Length));
        return aad;
    }

    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize) throw new ArgumentException("Key must be 32 bytes (AES-256).");
    }

    private static void ValidateMagic(ReadOnlySpan<byte> magic)
    {
        if (magic.Length != 4) throw new ArgumentException("Magic must be 4 bytes.");
    }
}
