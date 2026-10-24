using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace WhatSprintIsItWallpaper;

/// <summary>
/// Creates/removes the Windows Scheduled Task that keeps the wallpaper current. The task runs
/// <c>WhatSprintIsItWallpaper.exe run</c> shortly after logon and once daily (with "start when
/// available" so a machine that was off still catches up). Implemented with <c>schtasks.exe</c>
/// + an XML definition.
/// </summary>
[SupportedOSPlatform("windows")]
public static class TaskSchedulerManager
{
    public const string TaskName = "WhatSprintIsItWallpaper";

    public static void Install(string exePath)
    {
        string user = CurrentUser();
        string xml = BuildTaskXml(exePath, user);

        string tempPath = Path.Combine(Path.GetTempPath(), $"wsw-task-{Guid.NewGuid():N}.xml");
        // schtasks /XML requires a UTF-16 (Unicode) file.
        File.WriteAllText(tempPath, xml, new UnicodeEncoding(bigEndian: false, byteOrderMark: true));
        try
        {
            RunSchtasks($"/Create /TN \"{TaskName}\" /XML \"{tempPath}\" /F");
            Log.Info($"Scheduled task '{TaskName}' registered for {user}.");
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* ignore */ }
        }
    }

    public static void Uninstall()
    {
        // /F so removing a non-existent task doesn't error out an uninstaller.
        var (exitCode, output) = TryRunSchtasks($"/Delete /TN \"{TaskName}\" /F");
        if (exitCode == 0)
            Log.Info($"Scheduled task '{TaskName}' removed.");
        else
            Log.Warn($"Removing scheduled task returned {exitCode}: {output.Trim()}");
    }

    private static string CurrentUser()
    {
        string domain = Environment.UserDomainName;
        string name = Environment.UserName;
        return string.IsNullOrEmpty(domain) ? name : $"{domain}\\{name}";
    }

    private static string BuildTaskXml(string exePath, string user)
    {
        string command = SecurityElement.Escape(exePath) ?? exePath;
        string userEsc = SecurityElement.Escape(user) ?? user;

        return $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Description>Updates the desktop wallpaper to the current sprint (WhatSprintIsItWallpaper).</Description>
                <URI>\{TaskName}</URI>
              </RegistrationInfo>
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <UserId>{userEsc}</UserId>
                  <Delay>PT15S</Delay>
                </LogonTrigger>
                <CalendarTrigger>
                  <StartBoundary>2020-01-01T00:05:00</StartBoundary>
                  <Enabled>true</Enabled>
                  <ScheduleByDay>
                    <DaysInterval>1</DaysInterval>
                  </ScheduleByDay>
                </CalendarTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{userEsc}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>LeastPrivilege</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <AllowHardTerminate>true</AllowHardTerminate>
                <StartWhenAvailable>true</StartWhenAvailable>
                <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                <AllowStartOnDemand>true</AllowStartOnDemand>
                <Enabled>true</Enabled>
                <Hidden>false</Hidden>
                <RunOnlyIfIdle>false</RunOnlyIfIdle>
                <WakeToRun>false</WakeToRun>
                <ExecutionTimeLimit>PT5M</ExecutionTimeLimit>
                <Priority>7</Priority>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{command}</Command>
                  <Arguments>run</Arguments>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    private static void RunSchtasks(string arguments)
    {
        var (exitCode, output) = TryRunSchtasks(arguments);
        if (exitCode != 0)
            throw new InvalidOperationException($"schtasks {arguments} failed (exit {exitCode}): {output.Trim()}");
    }

    private static (int ExitCode, string Output) TryRunSchtasks(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start schtasks.exe.");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
    }
}
