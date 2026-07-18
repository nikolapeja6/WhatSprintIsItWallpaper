namespace WhatSprintIsItWallpaper;

/// <summary>Result of a sprint calculation: the sprint number and the 1-based week within it.</summary>
public readonly record struct SprintInfo(int Sprint, int Week)
{
    /// <summary>The display text, e.g. "278.1" (sprint "." week).</summary>
    public string Text => $"{Sprint}.{Week}";

    public override string ToString() => Text;
}

/// <summary>
/// Computes the current Azure DevOps sprint using the same offline formula as
/// https://whatsprintis.it/ — a fixed UTC epoch and a fixed number of weeks per sprint.
/// No network access or authentication is required.
/// </summary>
public sealed class SprintCalculator
{
    /// <summary>Milliseconds in one week (7 * 24 * 60 * 60 * 1000).</summary>
    private const long WeekMs = 604_800_000L;

    private readonly long _epochUnixMs;
    private readonly int _weeksPerSprint;

    /// <summary>whatsprintis.it default epoch: 2010-07-24 00:00:00 UTC (a Saturday).</summary>
    public static readonly DateTime DefaultEpochUtc = new(2010, 7, 24, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>whatsprintis.it default sprint length.</summary>
    public const int DefaultWeeksPerSprint = 3;

    public SprintCalculator(DateTime epochUtc, int weeksPerSprint)
    {
        if (weeksPerSprint < 1)
            throw new ArgumentOutOfRangeException(nameof(weeksPerSprint), "weeksPerSprint must be at least 1.");

        _epochUnixMs = new DateTimeOffset(DateTime.SpecifyKind(epochUtc, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        _weeksPerSprint = weeksPerSprint;
    }

    /// <summary>A calculator using the whatsprintis.it defaults.</summary>
    public static SprintCalculator Default { get; } = new(DefaultEpochUtc, DefaultWeeksPerSprint);

    /// <summary>Computes the sprint/week for the given instant.</summary>
    public SprintInfo GetSprint(DateTimeOffset instant)
    {
        long deltaMs = instant.ToUnixTimeMilliseconds() - _epochUnixMs;
        long weeks = (long)Math.Floor(deltaMs / (double)WeekMs);

        int sprint = (int)Math.Floor(weeks / (double)_weeksPerSprint);
        // Non-negative modulo so week is always in 1.._weeksPerSprint, even before the epoch.
        int weekIndex = (int)(((weeks % _weeksPerSprint) + _weeksPerSprint) % _weeksPerSprint);
        return new SprintInfo(sprint, weekIndex + 1);
    }

    /// <summary>Computes the sprint/week for the current UTC time.</summary>
    public SprintInfo GetCurrent() => GetSprint(DateTimeOffset.UtcNow);
}
