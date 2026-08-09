[← Architecture](ARCHITECTURE.md) · [Back to README](README.md) · [Development →](DEVELOPMENT.md) · [Русский](CONFIGURATION_RU.md)

# Configuration

## Local data

Settings are stored as JSON at:

```text
%AppData%\SmoothScroll\settings.json
```

If reading fails, the app saves a neighbouring backup with a `.yyyyMMddHHmmss.bak` suffix and loads valid defaults. Do not alter this file during diagnostics without the user's explicit permission.

## Main settings

| Group | Settings |
|---|---|
| Enablement | `IsEnabled`, automatic full-screen exclusion, and application-list mode |
| Scroll physics | distance, friction, acceleration, direction-change damping, maximum velocity, and stop threshold |
| Precision | precision multiplier, horizontal scrolling, and bypass keys |
| Profiles | named user-defined sets of scroll parameters |
| Application rules | process, executable path, enabled state, delivery mode, and profile |
| Window and tray | close to tray, start minimized, and Windows startup |
| Appearance | `System`, `Light`, or `Dark` |

Numeric values are snapped to their allowed ranges and steps during validation. Use the UI or close the app before manually editing JSON.

## Application rules

- **Exclusions:** smoothing is on everywhere except rules that disable it.
- **Selected applications only:** smoothing works only for enabled matching rules.

An `ExecutablePath` rule is more specific and is checked before a `ProcessName` rule. Rules created through the UI are marked as user rules.

## Import and export

The Settings page can export the current configuration to JSON and import it later. Imported data is validated; duplicate scroll-profile names are rejected.

## Environment and build configuration

The app has no required environment variables. Target framework and dependencies are declared in the `.csproj` files; debug launch settings are in `SmoothScrollModern/Properties/launchSettings.json`.

## See Also

- [Architecture](ARCHITECTURE.md) — runtime configuration snapshots.
- [Development](DEVELOPMENT.md) — safe changes to settings models.
- [Troubleshooting](TROUBLESHOOTING.md) — recovery after startup problems.
