[Back to README](README.md) · [Architecture →](ARCHITECTURE.md) · [Русский](GETTING-STARTED_RU.md)

# Getting Started

## Requirements

| Component | Requirement |
|---|---|
| OS | Windows 10 version 1809 (17763) or newer |
| SDK | .NET SDK compatible with `net10.0-windows10.0.19041.0` |
| Shell | PowerShell or the Visual Studio terminal |

The app uses WinUI 3 and Windows APIs, so macOS and Linux are not supported.

## Build

From the repository root, run:

```powershell
dotnet restore SmoothScrollModern.slnx
dotnet build SmoothScrollModern.slnx
```

A successful build finishes without errors and builds the four runtime projects plus the test project.

## First run

```powershell
dotnet run --project SmoothScrollModern/SmoothScrollModern.csproj --configuration Debug
```

1. Open Settings and confirm that smooth scrolling is enabled.
2. Adjust a profile or add an application exception if necessary.
3. Switch to another window and scroll its content with the mouse wheel.
4. Use the notification-area icon to control the app in the background.

## Verify it works

SmoothScroll excludes its own process by default. Test in another application with scrollable content. If smoothing is not applied, review the application-list mode and rules in [Configuration](CONFIGURATION.md).

## See Also

- [Architecture](ARCHITECTURE.md) — layers and the scroll-event path.
- [Configuration](CONFIGURATION.md) — profiles, settings, and exclusions.
- [Troubleshooting](TROUBLESHOOTING.md) — build and startup help.
