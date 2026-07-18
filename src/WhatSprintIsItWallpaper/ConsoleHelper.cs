using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WhatSprintIsItWallpaper;

/// <summary>
/// Because the app is built as a Windows (GUI) subsystem executable, it has no console of its own.
/// For interactive commands we attach to the parent terminal so messages are visible when run by hand.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ConsoleHelper
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    public static void AttachParent()
    {
        try
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
        }
        catch
        {
            // No parent console (e.g. launched by the installer/scheduler); file logging still works.
        }
    }
}
