using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace WhatSprintIsItWallpaper;

/// <summary>
/// Applies a wallpaper image to the desktop. Prefers the modern <c>IDesktopWallpaper</c> COM
/// interface (per-monitor, sets the fill color) and falls back to <c>SystemParametersInfo</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WallpaperSetter
{
    public static void Set(string imagePath, Color backgroundColor, string positionName)
    {
        DesktopWallpaperPosition position = ParsePosition(positionName);
        try
        {
            SetViaDesktopWallpaper(imagePath, backgroundColor, position);
            Log.Info($"Wallpaper applied via IDesktopWallpaper (position {position}).");
        }
        catch (Exception ex)
        {
            Log.Warn($"IDesktopWallpaper failed ({ex.Message}); falling back to SystemParametersInfo.");
            SetViaSystemParametersInfo(imagePath, backgroundColor, position);
            Log.Info($"Wallpaper applied via SystemParametersInfo (position {position}).");
        }
    }

    private static void SetViaDesktopWallpaper(string imagePath, Color bg, DesktopWallpaperPosition position)
    {
        var wallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
        try
        {
            wallpaper.SetBackgroundColor(ToColorRef(bg));
            wallpaper.SetPosition(position);
            // null monitor id => apply to every monitor.
            wallpaper.SetWallpaper(null, imagePath);
        }
        finally
        {
            Marshal.FinalReleaseComObject(wallpaper);
        }
    }

    private static void SetViaSystemParametersInfo(string imagePath, Color bg, DesktopWallpaperPosition position)
    {
        (string style, string tile) = RegistryStyleFor(position);
        using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true))
        {
            if (key is not null)
            {
                key.SetValue("WallpaperStyle", style);
                key.SetValue("TileWallpaper", tile);
            }
        }

        using (var colors = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", writable: true))
        {
            colors?.SetValue("Background", $"{bg.R} {bg.G} {bg.B}");
        }

        SetSysColors(1, new[] { COLOR_BACKGROUND }, new[] { (int)ToColorRef(bg) });

        if (SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE) == 0)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    /// <summary>COLORREF is 0x00BBGGRR.</summary>
    private static uint ToColorRef(Color c) => (uint)(c.R | (c.G << 8) | (c.B << 16));

    private static DesktopWallpaperPosition ParsePosition(string? name) =>
        name?.Trim().ToLowerInvariant() switch
        {
            "fill" => DesktopWallpaperPosition.Fill,
            "fit" => DesktopWallpaperPosition.Fit,
            "center" or "centre" => DesktopWallpaperPosition.Center,
            "stretch" => DesktopWallpaperPosition.Stretch,
            "span" => DesktopWallpaperPosition.Span,
            "tile" => DesktopWallpaperPosition.Tile,
            _ => DesktopWallpaperPosition.Fit,
        };

    /// <summary>Maps a position to the HKCU\Control Panel\Desktop (WallpaperStyle, TileWallpaper) values.</summary>
    private static (string Style, string Tile) RegistryStyleFor(DesktopWallpaperPosition position) =>
        position switch
        {
            DesktopWallpaperPosition.Center => ("0", "0"),
            DesktopWallpaperPosition.Tile => ("0", "1"),
            DesktopWallpaperPosition.Stretch => ("2", "0"),
            DesktopWallpaperPosition.Fit => ("6", "0"),
            DesktopWallpaperPosition.Fill => ("10", "0"),
            DesktopWallpaperPosition.Span => ("22", "0"),
            _ => ("6", "0"),
        };

    // ---- SystemParametersInfo fallback ----

    private const uint SPI_SETDESKWALLPAPER = 0x0014;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDWININICHANGE = 0x02;
    private const int COLOR_BACKGROUND = 1;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetSysColors(int cElements, int[] lpaElements, int[] lpaRgbValues);

    // ---- IDesktopWallpaper COM interop (Windows 8+) ----

    private enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    // CLSID_DesktopWallpaper
    [ComImport]
    [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    private class DesktopWallpaperClass
    {
    }

    // IID_IDesktopWallpaper. Method order MUST match the vtable exactly; unused methods are
    // declared as placeholders so the slots for the ones we call line up correctly.
    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
                          [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);

        uint GetMonitorDevicePathCount();

        Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

        void SetBackgroundColor(uint color);

        uint GetBackgroundColor();

        void SetPosition(DesktopWallpaperPosition position);

        DesktopWallpaperPosition GetPosition();
    }
}
