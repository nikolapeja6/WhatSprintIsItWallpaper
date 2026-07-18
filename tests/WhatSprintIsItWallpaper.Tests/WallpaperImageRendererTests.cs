using System.Drawing;
using WhatSprintIsItWallpaper;

namespace WhatSprintIsItWallpaper.Tests;

public class WallpaperImageRendererTests
{
    [Fact]
    public void Render_Writes_Png_With_Expected_Size_Background_And_Text()
    {
        var config = new AppConfig { BackgroundColor = "#1E1E1E", ForegroundColor = "#D4D4D4" };
        var sprint = new SprintInfo(278, 1);
        string path = Path.Combine(Path.GetTempPath(), $"wsw-test-{Guid.NewGuid():N}.png");

        try
        {
            WallpaperImageRenderer.Render(path, sprint, 1920, 1080, config);

            Assert.True(File.Exists(path));
            using var bmp = new Bitmap(path);
            Assert.Equal(1920, bmp.Width);
            Assert.Equal(1080, bmp.Height);

            // A corner should be the dark-grey background (~#1E1E1E).
            Color corner = bmp.GetPixel(3, 3);
            Assert.InRange((int)corner.R, 0x16, 0x26);
            Assert.InRange((int)corner.G, 0x16, 0x26);
            Assert.InRange((int)corner.B, 0x16, 0x26);

            // The centered "278.1" text should produce light pixels near the middle.
            bool foundLight = false;
            for (int y = bmp.Height / 2 - 120; y <= bmp.Height / 2 + 120 && !foundLight; y += 2)
            {
                for (int x = bmp.Width / 2 - 350; x <= bmp.Width / 2 + 350; x += 2)
                {
                    Color p = bmp.GetPixel(x, y);
                    if (p.R > 0x90 && p.G > 0x90 && p.B > 0x90)
                    {
                        foundLight = true;
                        break;
                    }
                }
            }

            Assert.True(foundLight, "Expected light text pixels near the center of the wallpaper.");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Theory]
    [InlineData("#1E1E1E", 0x1E, 0x1E, 0x1E)]
    [InlineData("D4D4D4", 0xD4, 0xD4, 0xD4)]
    [InlineData("#FF8800", 0xFF, 0x88, 0x00)]
    public void ParseHexColor_Parses_Valid_Values(string input, int r, int g, int b)
    {
        Color c = WallpaperImageRenderer.ParseHexColor(input, Color.Black);
        Assert.Equal((r, g, b), ((int)c.R, (int)c.G, (int)c.B));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("#12")]
    public void ParseHexColor_Falls_Back_On_Invalid(string? input)
    {
        Color fallback = Color.FromArgb(1, 2, 3);
        Color c = WallpaperImageRenderer.ParseHexColor(input, fallback);
        Assert.Equal((1, 2, 3), ((int)c.R, (int)c.G, (int)c.B));
    }
}
