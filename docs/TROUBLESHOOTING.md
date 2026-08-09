[← Testing](TESTING.md) · [Back to README](README.md) · [Русский](TROUBLESHOOTING_RU.md)

# Troubleshooting

## Restore or build fails

```powershell
dotnet restore SmoothScrollModern.slnx
dotnet build SmoothScrollModern.slnx
```

Confirm that a .NET SDK compatible with `net10.0-windows10.0.19041.0` is installed and that the command runs from the repository root. For more MSBuild detail:

```powershell
dotnet build SmoothScrollModern.slnx --verbosity normal
```

## App does not start

Run the WinUI executable project, not a class library:

```powershell
dotnet run --project SmoothScrollModern/SmoothScrollModern.csproj --configuration Debug
```

If startup throws an exception, the app displays a Windows message. Rebuild first and examine the error without changing the user's settings file blindly.

## Scrolling is not smoothed

Check whether smoothing is enabled, tray pause is inactive, the target is not automatically excluded full screen, and the selected application mode has a matching enabled rule. The SmoothScroll process itself is intentionally excluded.

## Settings are damaged

The JSON service automatically creates a `.bak` backup and resumes with defaults. Import a previously exported JSON file if personal settings must be restored.

## What to include in a bug report

Include Windows version, `dotnet --info`, the reproducing command, and the exception or build-error text. Do not publish exported settings if they contain sensitive paths or application names.

## See Also

- [Getting Started](GETTING-STARTED.md) — build and launch.
- [Configuration](CONFIGURATION.md) — settings and application rules.
- [Testing](TESTING.md) — automated and manual verification.
