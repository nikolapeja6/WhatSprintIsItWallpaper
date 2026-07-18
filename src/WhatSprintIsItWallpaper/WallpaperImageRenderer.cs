using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.Versioning;

namespace WhatSprintIsItWallpaper;

/// <summary>Renders the wallpaper PNG: a solid dark background with big centered sprint text.</summary>
[SupportedOSPlatform("windows")]
public static class WallpaperImageRenderer
{
    /// <summary>Renders the wallpaper for <paramref name="sprint"/> to <paramref name="filePath"/> (PNG).</summary>
    public static void Render(string filePath, SprintInfo sprint, int width, int height, AppConfig config)
    {
        if (width <= 0) width = 1920;
        if (height <= 0) height = 1080;

        Color bg = ParseHexColor(config.BackgroundColor, Color.FromArgb(0x1E, 0x1E, 0x1E));
        Color fg = ParseHexColor(config.ForegroundColor, Color.FromArgb(0xD4, 0xD4, 0xD4));
        FontStyle style = config.FontBold ? FontStyle.Bold : FontStyle.Regular;
        string family = string.IsNullOrWhiteSpace(config.FontFamily) ? "Segoe UI" : config.FontFamily;
        string mainText = sprint.Text;

        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(bg);

        float maxWidth = width * 0.70f;
        float maxHeight = height * (config.ShowCaption ? 0.42f : 0.48f);
        float fontSize = FitFontSize(g, mainText, family, style, maxWidth, maxHeight);

        using var font = new Font(family, fontSize, style, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(fg);
        using var centerFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        var fullRect = new RectangleF(0, 0, width, height);
        g.DrawString(mainText, font, brush, fullRect, centerFormat);

        if (config.ShowCaption)
        {
            string caption = $"sprint {sprint.Sprint}   \u00B7   week {sprint.Week}";
            using var capFont = new Font(family, Math.Max(fontSize * 0.14f, 12f), FontStyle.Regular, GraphicsUnit.Pixel);
            using var capBrush = new SolidBrush(Color.FromArgb(190, fg));

            float mainHeight = g.MeasureString(mainText, font).Height;
            float capTop = (height / 2f) + (mainHeight / 2f) - (mainHeight * 0.06f);
            var capRect = new RectangleF(0, capTop, width, height - capTop);
            using var capFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
            };
            g.DrawString(caption, capFont, capBrush, capRect, capFormat);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        bmp.Save(filePath, ImageFormat.Png);
    }

    /// <summary>Binary-searches the largest pixel font size whose text fits the given box.</summary>
    private static float FitFontSize(Graphics g, string text, string family, FontStyle style, float maxWidth, float maxHeight)
    {
        float lo = 8f;
        float hi = Math.Max(maxHeight, 16f);

        for (int i = 0; i < 24; i++)
        {
            float mid = (lo + hi) / 2f;
            using var font = new Font(family, mid, style, GraphicsUnit.Pixel);
            SizeF size = g.MeasureString(text, font);
            if (size.Width <= maxWidth && size.Height <= maxHeight)
                lo = mid;
            else
                hi = mid;
        }

        return lo;
    }

    /// <summary>Parses "#RRGGBB" (or "RRGGBB"); returns <paramref name="fallback"/> on failure.</summary>
    public static Color ParseHexColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string s = value.Trim().TrimStart('#');
        if (s.Length == 6 &&
            int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        return fallback;
    }
}
