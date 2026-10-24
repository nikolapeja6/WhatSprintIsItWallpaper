using WhatSprintIsItWallpaper;

namespace WhatSprintIsItWallpaper.Tests;

public class DisplayConfigurationTests
{
    private static readonly DisplayMonitor Primary =
        new(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);

    private static readonly DisplayMonitor Secondary =
        new(@"\\.\DISPLAY2", 1920, 0, 4480, 1440, false);

    [Fact]
    public void CreateSignature_Is_Independent_Of_Enumeration_Order()
    {
        string first = DisplayConfiguration.CreateSignature([Primary, Secondary]);
        string second = DisplayConfiguration.CreateSignature([Secondary, Primary]);

        Assert.Equal(first, second);
    }

    [Fact]
    public void CreateSignature_Changes_With_Monitor_Topology()
    {
        string original = DisplayConfiguration.CreateSignature([Primary, Secondary]);

        Assert.NotEqual(original, DisplayConfiguration.CreateSignature([Primary]));
        Assert.NotEqual(original, DisplayConfiguration.CreateSignature([
            Primary,
            Secondary with { Left = -2560, Right = 0 },
        ]));
        Assert.NotEqual(original, DisplayConfiguration.CreateSignature([
            Primary,
            Secondary with { Bottom = 1600 },
        ]));
        Assert.NotEqual(original, DisplayConfiguration.CreateSignature([
            Primary with { IsPrimary = false },
            Secondary with { IsPrimary = true },
        ]));
    }
}
