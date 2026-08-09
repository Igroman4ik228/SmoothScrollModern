# SmoothScroll

> A native Windows app that makes mouse-wheel scrolling smooth and configurable for every application.

SmoothScroll captures global wheel input, selects a profile for the active window, and sends smoothed scroll events. Settings, exceptions, and profiles are managed in the app and stored locally.

[Русская версия](README_RU.md)

## Quick start

Run these commands from the repository root:

```powershell
dotnet restore SmoothScrollModern.slnx
dotnet build SmoothScrollModern.slnx
dotnet run --project SmoothScrollModern/SmoothScrollModern.csproj --configuration Debug
```

After launch, open the app window or its notification-area icon, enable smooth scrolling, and test it in another application.

## Features

- Global smooth vertical and horizontal scrolling.
- Adjustable distance, friction, acceleration, speed limit, and precision.
- Scroll profiles and rules for processes or executable paths.
- Exclusion and selected-applications-only modes.
- Tray controls, Windows startup, and temporary pause.
- Import, export, and recovery of local settings.

## How it works

```text
Mouse wheel → global hook → window rule selection → smoothing engine → Win32 wheel event
```

An executable-path rule takes priority over a process-name rule. Full-screen applications can be excluded automatically.

## Documentation

| Guide | Description |
|---|---|
| [Getting Started](GETTING-STARTED.md) | Requirements, build, and first run |
| [Architecture](ARCHITECTURE.md) | Layers, data flow, and dependency boundaries |
| [Configuration](CONFIGURATION.md) | Settings, profiles, rules, and local storage |
| [Development](DEVELOPMENT.md) | Solution structure and development workflow |
| [Testing](TESTING.md) | Test suite and verification commands |
| [Troubleshooting](TROUBLESHOOTING.md) | Common build and runtime issues |

## License

The repository does not currently include a license file. Until one is added, use is governed by the repository owner.
