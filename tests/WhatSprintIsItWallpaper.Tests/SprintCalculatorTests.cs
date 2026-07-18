using WhatSprintIsItWallpaper;

namespace WhatSprintIsItWallpaper.Tests;

public class SprintCalculatorTests
{
    private static DateTimeOffset Utc(string iso) =>
        DateTimeOffset.Parse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

    // The canonical value the user gave and the live site confirmed: today's sprint is 278.1.
    [Fact]
    public void Today_2026_07_18_Is_278_1()
    {
        var info = SprintCalculator.Default.GetSprint(Utc("2026-07-18T00:00:00Z"));
        Assert.Equal(278, info.Sprint);
        Assert.Equal(1, info.Week);
        Assert.Equal("278.1", info.Text);
    }

    // Sprints roll the week over every Saturday 00:00 UTC; the sprint number bumps every 3rd Saturday.
    [Theory]
    [InlineData("2026-07-18T00:00:00Z", "278.1")] // Saturday: week 1 starts
    [InlineData("2026-07-21T12:00:00Z", "278.1")] // mid week 1
    [InlineData("2026-07-24T23:59:59Z", "278.1")] // last second before the next Saturday
    [InlineData("2026-07-25T00:00:00Z", "278.2")] // Saturday: week 2 starts
    [InlineData("2026-08-01T00:00:00Z", "278.3")] // Saturday: week 3 starts
    [InlineData("2026-08-08T00:00:00Z", "279.1")] // Saturday: next sprint, week 1
    public void Rolls_Over_On_Saturday_Utc_Boundaries(string instant, string expected)
    {
        Assert.Equal(expected, SprintCalculator.Default.GetSprint(Utc(instant)).Text);
    }

    // The epoch itself is sprint 0, week 1.
    [Theory]
    [InlineData("2010-07-24T00:00:00Z", "0.1")] // epoch
    [InlineData("2010-07-31T00:00:00Z", "0.2")] // +1 week
    [InlineData("2010-08-07T00:00:00Z", "0.3")] // +2 weeks
    [InlineData("2010-08-14T00:00:00Z", "1.1")] // +3 weeks -> sprint 1
    public void Epoch_Anchors_At_Sprint_Zero(string instant, string expected)
    {
        Assert.Equal(expected, SprintCalculator.Default.GetSprint(Utc(instant)).Text);
    }

    [Fact]
    public void Week_Is_Always_Between_One_And_WeeksPerSprint()
    {
        var calc = SprintCalculator.Default;
        var start = Utc("2026-07-18T00:00:00Z");
        for (int day = 0; day < 63; day++) // 3 sprints worth of days
        {
            int week = calc.GetSprint(start.AddDays(day)).Week;
            Assert.InRange(week, 1, SprintCalculator.DefaultWeeksPerSprint);
        }
    }

    [Fact]
    public void Custom_Config_Is_Honored()
    {
        // 2-week sprints anchored at 2020-01-04 (a Saturday).
        var calc = new SprintCalculator(new DateTime(2020, 1, 4, 0, 0, 0, DateTimeKind.Utc), 2);
        Assert.Equal("0.1", calc.GetSprint(Utc("2020-01-04T00:00:00Z")).Text);
        Assert.Equal("0.2", calc.GetSprint(Utc("2020-01-11T00:00:00Z")).Text);
        Assert.Equal("1.1", calc.GetSprint(Utc("2020-01-18T00:00:00Z")).Text);
    }

    [Fact]
    public void Invalid_WeeksPerSprint_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SprintCalculator(SprintCalculator.DefaultEpochUtc, 0));
    }
}
