using System;
using System.IO;

namespace MagicCursor;

internal static class Log
{
    private static readonly object _lock = new();
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MagicCursor",
        "logs"
    );
    private static readonly string LogPath = Path.Combine(LogDir, "magiccursor.log");
    private static readonly string BackupPath = Path.Combine(LogDir, "magiccursor.log.1");
    private const long MaxSizeBytes = 1024 * 1024; // 1 MB

    public static void Info(string msg)
    {
        Write("INFO", msg, null);
    }

    public static void Error(string msg, Exception? ex = null)
    {
        Write("ERROR", msg, ex);
    }

    private static void Write(string level, string msg, Exception? ex)
    {
        try
        {
            lock (_lock)
            {
                if (!Directory.Exists(LogDir))
                {
                    Directory.CreateDirectory(LogDir);
                }

                // Rotate if the current log file meets or exceeds 1 MB
                if (File.Exists(LogPath))
                {
                    var fileInfo = new FileInfo(LogPath);
                    if (fileInfo.Length >= MaxSizeBytes)
                    {
                        try
                        {
                            if (File.Exists(BackupPath))
                            {
                                File.Delete(BackupPath);
                            }
                            File.Move(LogPath, BackupPath);
                        }
                        catch
                        {
                            // If rotation fails, proceed with appending to avoid dropping logs
                        }
                    }
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string line = $"{timestamp} [{level}] {msg}";
                if (ex != null)
                {
                    line += $"{Environment.NewLine}{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                }

                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw
        }
    }
}
