namespace WhatSprintIsItWallpaper;

/// <summary>
/// Minimal logger: always appends to <see cref="AppPaths.LogFile"/> and, when a console is
/// attached (see <c>ConsoleHelper</c>), also writes to stdout/stderr.
/// </summary>
public static class Log
{
    private static readonly object Gate = new();

    public static void Info(string message) => Write("INFO", message, error: false);
    public static void Warn(string message) => Write("WARN", message, error: false);
    public static void Error(string message) => Write("ERROR", message, error: true);

    private static void Write(string level, string message, bool error)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff K} [{level}] {message}";
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                File.AppendAllText(AppPaths.LogFile, line + Environment.NewLine);
                TrimIfLarge();
            }
            catch
            {
                // Logging must never crash the app.
            }

            try
            {
                (error ? Console.Error : Console.Out).WriteLine(message);
            }
            catch
            {
                // No console attached; ignore.
            }
        }
    }

    private static void TrimIfLarge()
    {
        try
        {
            var fi = new FileInfo(AppPaths.LogFile);
            if (fi.Exists && fi.Length > 256 * 1024)
            {
                var lines = File.ReadAllLines(AppPaths.LogFile);
                File.WriteAllLines(AppPaths.LogFile, lines[^Math.Min(lines.Length, 500)..]);
            }
        }
        catch
        {
            // ignore
        }
    }
}
