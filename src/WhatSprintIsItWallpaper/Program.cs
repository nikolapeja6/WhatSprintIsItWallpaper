using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;

namespace WhatSprintIsItWallpaper;

[SupportedOSPlatform("windows")]
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string command = (args.Length > 0 ? args[0] : "run").Trim().ToLowerInvariant();

        // Interactive commands attach to the parent terminal so their output is visible.
        if (command is not "run")
            ConsoleHelper.AttachParent();

        try
        {
            return command switch
            {
                "run" or "update" => CmdRun(force: false),
                "install" => CmdInstall(),
                "uninstall" => CmdUninstall(),
                "preview" => CmdPreview(),
                "-h" or "--help" or "help" or "/?" => CmdHelp(),
                _ => UnknownCommand(command),
            };
        }
        catch (Exception ex)
        {
            Log.Error($"'{command}' failed: {ex}");
            return 1;
        }
    }

    /// <summary>Computes the current sprint, renders the image if needed, and applies it as wallpaper.</summary>
    private static int CmdRun(bool force)
    {
        var config = AppConfig.Load();
        SprintInfo sprint = config.CreateCalculator().GetCurrent();
        DisplaySnapshot displays = DisplayConfiguration.Capture();
        (int width, int height) = ResolveSize(config, displays);
        string imagePath = AppPaths.WallpaperFile(width, height);

        string signature = string.Join('|',
            sprint.Text, $"{width}x{height}", displays.Signature,
            config.BackgroundColor, config.ForegroundColor,
            config.ShowCaption, config.FontFamily, config.FontBold, config.WallpaperPosition);

        var state = AppState.Load();
        if (!force && File.Exists(imagePath) && state.Signature == signature)
        {
            Log.Info($"Already up to date: sprint {sprint.Text} ({width}x{height}). Nothing to do.");
            return 0;
        }

        Log.Info($"Rendering sprint {sprint.Text} at {width}x{height}.");
        WallpaperImageRenderer.Render(imagePath, sprint, width, height, config);

        Color bg = WallpaperImageRenderer.ParseHexColor(config.BackgroundColor, Color.FromArgb(0x1E, 0x1E, 0x1E));
        WallpaperSetter.Set(imagePath, bg, config.WallpaperPosition);

        state.Signature = signature;
        state.LastAppliedText = sprint.Text;
        state.LastAppliedUtc = DateTimeOffset.UtcNow;
        state.Save();

        Log.Info($"Desktop wallpaper set to sprint {sprint.Text}.");
        return 0;
    }

    private static int CmdInstall()
    {
        string exePath = ExePath();
        Log.Info($"Registering scheduled task for: {exePath}");
        TaskSchedulerManager.Install(exePath);
        CmdRun(force: true); // apply immediately so the wallpaper is correct right away
        Log.Info("Install complete. The wallpaper will now update automatically at logon and daily.");
        return 0;
    }

    private static int CmdUninstall()
    {
        TaskSchedulerManager.Uninstall();
        Log.Info("Uninstall complete. Your current wallpaper is left as-is.");
        return 0;
    }

    private static int CmdPreview()
    {
        var config = AppConfig.Load();
        SprintInfo sprint = config.CreateCalculator().GetCurrent();
        DisplaySnapshot displays = DisplayConfiguration.Capture();
        (int width, int height) = ResolveSize(config, displays);

        string previewPath = Path.Combine(
            Path.GetTempPath(), $"whatsprint-preview-{sprint.Sprint}.{sprint.Week}-{width}x{height}.png");
        WallpaperImageRenderer.Render(previewPath, sprint, width, height, config);
        Log.Info($"Preview for sprint {sprint.Text} written to: {previewPath}");

        try
        {
            Process.Start(new ProcessStartInfo(previewPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not open the preview automatically: {ex.Message}");
        }
        return 0;
    }

    private static int CmdHelp()
    {
        Console.WriteLine(
            """
            WhatSprintIsItWallpaper - sets the Windows desktop wallpaper to the current sprint (e.g. 278.1).

            Usage:
              WhatSprintIsItWallpaper [command]

            Commands:
              run         (default) Compute the current sprint and set the wallpaper if it changed.
              install     Register the scheduled task (logon + daily) and apply the wallpaper now.
              uninstall   Remove the scheduled task.
              preview     Render the current image to a temp file and open it, without changing the wallpaper.
              help        Show this help.

            Config file: %APPDATA%\WhatSprintIsItWallpaper\config.json
            Log file:    %LOCALAPPDATA%\WhatSprintIsItWallpaper\log.txt
            """);
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Log.Error($"Unknown command '{command}'.");
        CmdHelp();
        return 2;
    }

    private static (int Width, int Height) ResolveSize(AppConfig config, DisplaySnapshot displays)
    {
        if (config.Width is int w && w > 0 && config.Height is int h && h > 0)
            return (w, h);

        return (displays.PrimaryWidth, displays.PrimaryHeight);
    }

    private static string ExePath()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath) &&
            processPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            !processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        // Dev fallback (running via `dotnet run`): look next to the assembly.
        string candidate = Path.Combine(AppContext.BaseDirectory, "WhatSprintIsItWallpaper.exe");
        if (File.Exists(candidate))
            return candidate;

        return processPath ?? candidate;
    }
}
