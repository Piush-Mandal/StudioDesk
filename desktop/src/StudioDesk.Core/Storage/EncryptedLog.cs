using System.Text;

namespace StudioDesk.Core.Storage;

/// <summary>
/// One encrypted log file per app session in UserData/Logs. Keeps the newest 20 sessions.
/// Never log passwords, keys or personal content.
/// </summary>
public sealed class EncryptedLog : IDisposable
{
    private const int KeepSessions = 20;
    private readonly EncryptedStore _store;
    private readonly string _file;
    private readonly StringBuilder _buffer = new();
    private readonly Lock _lock = new();

    public EncryptedLog(EncryptedStore store, string logsDir)
    {
        _store = store;
        _file = $"Logs/{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";
        Prune(logsDir);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            _buffer.Append(DateTime.UtcNow.ToString("O")).Append(' ').Append(level).Append(' ').AppendLine(message);
            _store.WriteText(_file, _buffer.ToString());
        }
    }

    private static void Prune(string logsDir)
    {
        if (!Directory.Exists(logsDir)) return;
        foreach (var old in Directory.GetFiles(logsDir, "*.sdx").OrderByDescending(f => f).Skip(KeepSessions - 1))
            File.Delete(old);
    }

    public void Dispose() { }
}
