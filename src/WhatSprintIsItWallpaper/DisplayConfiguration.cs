using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WhatSprintIsItWallpaper;

public readonly record struct DisplayMonitor(
    string DeviceName,
    int Left,
    int Top,
    int Right,
    int Bottom,
    bool IsPrimary);

public readonly record struct DisplaySnapshot(int PrimaryWidth, int PrimaryHeight, string Signature);

/// <summary>Captures the active Windows monitor topology used to size and invalidate wallpapers.</summary>
[SupportedOSPlatform("windows")]
public static class DisplayConfiguration
{
    public static DisplaySnapshot Capture()
    {
        var monitors = new List<DisplayMonitor>();
        MonitorEnumProc callback = (monitor, _, _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                monitors.Add(new DisplayMonitor(
                    info.DeviceName ?? string.Empty,
                    info.Monitor.Left,
                    info.Monitor.Top,
                    info.Monitor.Right,
                    info.Monitor.Bottom,
                    (info.Flags & MonitorInfoPrimary) != 0));
            }

            return true;
        };

        bool enumerated = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        if (enumerated && monitors.Count > 0)
        {
            DisplayMonitor primary = monitors.FirstOrDefault(monitor => monitor.IsPrimary);
            if (!primary.IsPrimary)
                primary = monitors[0];

            return new DisplaySnapshot(
                Math.Max(primary.Right - primary.Left, 1),
                Math.Max(primary.Bottom - primary.Top, 1),
                CreateSignature(monitors));
        }

        int width = GetSystemMetrics(SystemMetricPrimaryWidth);
        int height = GetSystemMetrics(SystemMetricPrimaryHeight);
        if (width <= 0) width = 1920;
        if (height <= 0) height = 1080;

        var fallback = new DisplayMonitor("PRIMARY", 0, 0, width, height, true);
        return new DisplaySnapshot(width, height, CreateSignature([fallback]));
    }

    public static string CreateSignature(IEnumerable<DisplayMonitor> monitors) =>
        string.Join(';', monitors
            .OrderBy(monitor => monitor.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(monitor => monitor.Left)
            .ThenBy(monitor => monitor.Top)
            .ThenBy(monitor => monitor.Right)
            .ThenBy(monitor => monitor.Bottom)
            .ThenBy(monitor => monitor.IsPrimary)
            .Select(monitor => string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{monitor.DeviceName.ToUpperInvariant()}:{monitor.Left},{monitor.Top},{monitor.Right},{monitor.Bottom}:{(monitor.IsPrimary ? 1 : 0)}")));

    private const uint MonitorInfoPrimary = 0x00000001;
    private const int SystemMetricPrimaryWidth = 0;
    private const int SystemMetricPrimaryHeight = 1;

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string? DeviceName;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr clip,
        MonitorEnumProc callback,
        IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
