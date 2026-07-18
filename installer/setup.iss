; Inno Setup script for WhatSprintIsItWallpaper.
; Builds a per-user installer (no admin prompt) that installs the single self-contained exe,
; registers the auto-update scheduled task, and applies the wallpaper immediately.
;
; Build:  iscc /DMyAppVersion=1.2.3 installer\setup.iss
; (expects the published exe at ..\publish\win-x64\WhatSprintIsItWallpaper.exe)

#define MyAppName "WhatSprintIsItWallpaper"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "nikolapeja6"
#define MyAppURL "https://github.com/nikolapeja6/WhatSprintIsItWallpaper"
#define MyAppExe "WhatSprintIsItWallpaper.exe"

[Setup]
; Stable AppId — do not change between versions.
AppId={{7B2D9E14-4C3A-4F5B-9A21-8C0D2E7F1A93}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
; Per-user install: no elevation, so the scheduled task is created for the real logged-in user.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExe}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\win-x64\{#MyAppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; Register the scheduled task (logon + daily) and apply the wallpaper now.
Filename: "{app}\{#MyAppExe}"; Parameters: "install"; Flags: runhidden waituntilterminated; StatusMsg: "Setting up automatic sprint wallpaper..."

[UninstallRun]
; Remove the scheduled task before files are deleted.
Filename: "{app}\{#MyAppExe}"; Parameters: "uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveScheduledTask"
