# WhatSprintIsItWallpaper

Sets your **Windows desktop wallpaper** to the current sprint — a clean, dark image with big
centered text like **`278.1`** (sprint `.` week), inspired by [whatsprintis.it](https://whatsprintis.it/).

After a one-time install it keeps itself up to date **automatically**: a Windows Scheduled Task
runs at logon and once daily, so the number rolls over on its own every Saturday. No login, no
network, and no Azure DevOps account required — the sprint is computed with a fixed date formula.

> AI-generated Windows utility that sets your desktop background to the current Azure DevOps sprint and automatically updates it.

---

## How updates happen (automatic)

Installing registers a **Scheduled Task** named `WhatSprintIsItWallpaper` that runs
`WhatSprintIsItWallpaper.exe run`:

1. **Immediately at install** — the wallpaper is set the moment installation finishes.
2. **At every logon / sign-in** — always correct after a reboot.
3. **Once daily (~00:05)** with *start-when-available* — catches the Saturday rollover even on
   machines that were off or stayed logged in for weeks.

Each run is cheap and idempotent: it computes the sprint, compares it to a cached value, and only
re-renders and re-applies the wallpaper when the week/sprint actually changed.

## Install

Via **winget** (once the package is published to the community repo):

```powershell
winget install nikolapeja6.WhatSprintIsItWallpaper
```

Or download the latest `WhatSprintIsItWallpaper-<version>.msi` from the
[Releases](https://github.com/nikolapeja6/WhatSprintIsItWallpaper/releases) page and run
it. The installer is **per-user** and needs no administrator rights.

## Usage

The app is normally driven by the scheduled task, but you can run it manually:

| Command                          | What it does |
|----------------------------------|--------------|
| `WhatSprintIsItWallpaper run`        | (default) Compute the sprint and set the wallpaper if it changed. |
| `WhatSprintIsItWallpaper install`    | Register the scheduled task (logon + daily) and apply the wallpaper now. |
| `WhatSprintIsItWallpaper uninstall`  | Remove the scheduled task. |
| `WhatSprintIsItWallpaper preview`    | Render the current image to a temp file and open it, without changing the wallpaper. |
| `WhatSprintIsItWallpaper help`       | Show help. |

## Configuration

A JSON config is created on first run at `%APPDATA%\WhatSprintIsItWallpaper\config.json`:

```jsonc
{
  "BackgroundColor": "#1E1E1E",   // dark grey background
  "ForegroundColor": "#D4D4D4",   // text color
  "FontFamily": "Segoe UI",
  "FontBold": false,
  "ShowCaption": true,            // small "sprint N · week W" under the big number
  "EpochUtc": "2010-07-24",       // sprint anchor (UTC date)
  "WeeksPerSprint": 3,
  "Width": null,                  // null = use the primary monitor width
  "Height": null,                 // null = use the primary monitor height
  "WallpaperPosition": "Fit"      // Fit, Fill, Center, Stretch, Span, or Tile
}
```

Edit it and run `WhatSprintIsItWallpaper run` to apply. Logs are written to
`%LOCALAPPDATA%\WhatSprintIsItWallpaper\log.txt`.

## Build from source

Requires the .NET 8 SDK (a `global.json` pins the build to 8.0).

```powershell
dotnet build WhatSprintIsItWallpaper.sln -c Release      # build
dotnet test  WhatSprintIsItWallpaper.sln -c Release      # run unit tests

# Produce the self-contained single-file exe (no .NET runtime needed to run it):
dotnet publish src/WhatSprintIsItWallpaper/WhatSprintIsItWallpaper.csproj -c Release -r win-x64 `
  -p:SelfContained=true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o publish/win-x64
```

Build the installer (MSI) locally. It uses the [WiX Toolset](https://wixtoolset.org/) v5 via the
`WixToolset.Sdk` MSBuild SDK, so **no extra tooling beyond the .NET SDK is required** — WiX is
restored as a NuGet package. First publish the exe (above), then:

```powershell
dotnet build installer/WhatSprintIsItWallpaper.wixproj -c Release `
  -p:ProductVersion=1.0.0 -p:PublishDir=..\publish\win-x64
# -> installer/bin/Release/WhatSprintIsItWallpaper-1.0.0.msi
```

## Releasing & publishing to winget

1. Tag a version: `git tag v1.0.0 && git push origin v1.0.0`.
2. The [`Release`](.github/workflows/release.yml) workflow tests, publishes the exe, builds the
   WiX MSI, computes its SHA256 and ProductCode, and attaches the `.msi` to a GitHub Release.
3. Update the manifests in [`winget/`](winget/) with the new version, `InstallerUrl`,
   `InstallerSha256`, and `ProductCode` (all printed in the workflow summary), then validate:
   ```powershell
   winget validate --manifest winget
   ```
4. Open a PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) (or use
   `wingetcreate update nikolapeja6.WhatSprintIsItWallpaper -v <version> -u <installer-url> ...`).

## Uninstall

`winget uninstall nikolapeja6.WhatSprintIsItWallpaper`, or via **Settings → Apps**. Uninstalling
removes the scheduled task; your wallpaper is left as-is.

## How it works (internals)

| File | Responsibility |
|------|----------------|
| `SprintCalculator.cs` | The offline sprint/week formula. |
| `WallpaperImageRenderer.cs` | Renders the dark PNG with big auto-fit centered text (System.Drawing). |
| `WallpaperSetter.cs` | Applies the wallpaper via `IDesktopWallpaper` COM (fallback: `SystemParametersInfo`). |
| `TaskSchedulerManager.cs` | Registers/removes the scheduled task (logon + daily). |
| `AppConfig.cs` / `AppState.cs` | JSON config and the idempotent "last applied" cache. |
| `Program.cs` | CLI dispatch (`run`/`install`/`uninstall`/`preview`). |

## License

[MIT](LICENSE).

