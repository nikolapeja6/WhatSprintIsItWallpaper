using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatSprintIsItWallpaper;

/// <summary>Well-known on-disk locations used by the app.</summary>
public static class AppPaths
{
    public const string AppFolderName = "WhatSprintIsItWallpaper";

    /// <summary>Roaming config: %APPDATA%\WhatSprintIsItWallpaper.</summary>
    public static string ConfigDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);

    /// <summary>Local data (rendered images, state, log): %LOCALAPPDATA%\WhatSprintIsItWallpaper.</summary>
    public static string DataDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    public static string ConfigFile => Path.Combine(ConfigDir, "config.json");
    public static string StateFile => Path.Combine(DataDir, "state.json");
    public static string LogFile => Path.Combine(DataDir, "log.txt");

    public static string WallpaperFile(int width, int height) =>
        Path.Combine(DataDir, $"wallpaper-{width}x{height}.png");
}

/// <summary>User-editable configuration, persisted as JSON in <see cref="AppPaths.ConfigFile"/>.</summary>
public sealed class AppConfig
{
    /// <summary>Background color as "#RRGGBB". Default: dark grey.</summary>
    public string BackgroundColor { get; set; } = "#1E1E1E";

    /// <summary>Text color as "#RRGGBB". Default: light grey (whatsprintis.it dark theme).</summary>
    public string ForegroundColor { get; set; } = "#D4D4D4";

    /// <summary>Font family for the big text.</summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>Render the big text in bold.</summary>
    public bool FontBold { get; set; }

    /// <summary>Show a small "sprint N · week W" caption under the big number.</summary>
    public bool ShowCaption { get; set; } = true;

    /// <summary>Sprint epoch (UTC date, ISO "yyyy-MM-dd"). Default matches whatsprintis.it.</summary>
    public string EpochUtc { get; set; } = "2010-07-24";

    /// <summary>Weeks per sprint. Default 3 (whatsprintis.it).</summary>
    public int WeeksPerSprint { get; set; } = SprintCalculator.DefaultWeeksPerSprint;

    /// <summary>Optional fixed image width. When null or non-positive, the primary monitor width is used.</summary>
    public int? Width { get; set; }

    /// <summary>Optional fixed image height. When null or non-positive, the primary monitor height is used.</summary>
    public int? Height { get; set; }

    /// <summary>
    /// How the image is placed on each monitor: Fit, Fill, Center, Stretch, Span, or Tile.
    /// Default Fit — with a solid background it letterboxes seamlessly and never crops the text
    /// (best for mixed portrait/landscape monitors).
    /// </summary>
    public string WallpaperPosition { get; set; } = "Fit";

    [JsonIgnore]
    public DateTime EpochDateUtc
    {
        get
        {
            if (DateTime.TryParse(EpochUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return SprintCalculator.DefaultEpochUtc;
        }
    }

    public SprintCalculator CreateCalculator() =>
        new(EpochDateUtc, WeeksPerSprint < 1 ? SprintCalculator.DefaultWeeksPerSprint : WeeksPerSprint);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Loads config from disk, creating a default file on first run. Never throws.</summary>
    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(AppPaths.ConfigFile))
            {
                var json = File.ReadAllText(AppPaths.ConfigFile);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (cfg is not null)
                    return cfg;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read config, using defaults: {ex.Message}");
        }

        var fresh = new AppConfig();
        try { fresh.Save(); } catch { /* best effort */ }
        return fresh;
    }

    public void Save()
    {
        Directory.CreateDirectory(AppPaths.ConfigDir);
        File.WriteAllText(AppPaths.ConfigFile, JsonSerializer.Serialize(this, JsonOptions));
    }
}
