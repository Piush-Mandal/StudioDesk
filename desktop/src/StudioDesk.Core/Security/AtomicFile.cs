namespace StudioDesk.Core.Security;

/// <summary>Writes via a temp file + rename so a crash never leaves a half-written file.</summary>
internal static class AtomicFile
{
    public static void Write(string path, byte[] contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, contents);
        File.Move(temp, path, overwrite: true);
    }
}
