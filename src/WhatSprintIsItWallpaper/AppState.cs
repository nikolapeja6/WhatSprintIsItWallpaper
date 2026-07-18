using System.Text.Json;

namespace WhatSprintIsItWallpaper;

/// <summary>Persisted "last applied" state so runs are idempotent (only repaint when something changes).</summary>
public sealed class AppState
{
    public string? Signature { get; set; }
    public string? LastAppliedText { get; set; }
    public DateTimeOffset? LastAppliedUtc { get; set; }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppState Load()
    {
        try
        {
            if (File.Exists(AppPaths.StateFile))
                return JsonSerializer.Deserialize<AppState>(File.ReadAllText(AppPaths.StateFile)) ?? new AppState();
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read state: {ex.Message}");
        }
        return new AppState();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            File.WriteAllText(AppPaths.StateFile, JsonSerializer.Serialize(this, Options));
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not write state: {ex.Message}");
        }
    }
}
