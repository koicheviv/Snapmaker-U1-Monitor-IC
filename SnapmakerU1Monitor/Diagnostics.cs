
using System;
using System.IO;

namespace SnapmakerU1Monitor;

public static class Diagnostics
{
    private static readonly object LockObject = new();

    private static string LogFile =>
        Path.Combine(FileSystem.AppDataDirectory, "diagnostics.txt");

    public static void Log(string message)
    {
        try
        {
            lock (LockObject)
            {
                string entry =
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}";

                File.AppendAllText(LogFile, entry);
            }
        }
        catch
        {
            // Диагностиката не трябва да прекъсва приложението.
        }
    }

    public static void LogException(string location, Exception ex)
    {
        Log($"ERROR at {location}: {ex}");
    }

    public static string GetLogFilePath()
    {
        return LogFile;
    }
}
