[← Configuration](CONFIGURATION.md) · [Back to README](README.md) · [Testing →](TESTING.md) · [Русский](DEVELOPMENT_RU.md)

# Development

## Workflow

```powershell
dotnet build SmoothScrollModern.slnx
dotnet test SmoothScrollModern.slnx
```

After code or project-file changes, confirm that the build succeeds. Run tests before submitting changes to Application, Infrastructure, or settings models.

## Code placement

| Task | Location |
|---|---|
| Settings model and invariants | `SmoothScrollModern.Domain` |
| Pure logic, use case, or external-dependency contract | `SmoothScrollModern.Application` |
| Windows, IO, SharpHook, or Win32 implementation | `SmoothScrollModern.Infrastructure` |
| ViewModel, XAML, commands, and UI composition | `SmoothScrollModern` |
| Isolated behavior test | `SmoothScrollModern.Application.Tests` |

Start a new external integration with an Application contract, implement it in Infrastructure, and register it in `App.xaml.cs`.

## UI

Follow Fluent/WinUI patterns: use clear hierarchy, restrained surfaces, and compact XAML controls. `SettingsCard` and `SettingsExpander` are appropriate only for familiar Windows settings scenarios.

ViewModels own commands and state. Code-behind may handle a small UI-only bridge, never file IO, P/Invoke, or business logic.

## Native code and input

- Keep P/Invoke in `Infrastructure/Native` and wrap failures in useful exceptions or results.
- Do not call Windows APIs from Domain, Application, or XAML controls.
- Do not hold locks while delivering an external wheel event.
- Release native and cancellable resources through `AppBootstrapper` lifecycle management.

## See Also

- [Testing](TESTING.md) — verify changes.
- [Architecture](ARCHITECTURE.md) — dependency boundaries.
- [Configuration](CONFIGURATION.md) — settings-model rules.
