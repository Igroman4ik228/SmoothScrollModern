[← Getting Started](GETTING-STARTED.md) · [Back to README](README.md) · [Configuration →](CONFIGURATION.md) · [Русский](ARCHITECTURE_RU.md)

# Application Architecture

## Layers

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `SmoothScrollModern.Domain` | Settings models, profiles, rules, and validation |
| Application | `SmoothScrollModern.Application` | Scroll decisions, physics, snapshots, and contracts |
| Infrastructure | `SmoothScrollModern.Infrastructure` | SharpHook, Win32, JSON, tray, and Windows startup |
| Presentation | `SmoothScrollModern` | WinUI 3, pages, ViewModels, and dependency injection |

Dependencies flow as `Presentation → Application → Domain`. Infrastructure implements Application contracts and can use Domain. Detailed development boundaries are in the [architecture context](../.ai-factory/ARCHITECTURE.md).

## Scroll flow

```text
Wheel event → SharpHookInputService → ScrollDecisionService
→ SmoothScrollEngine → IInputInjectionService → WindowsWheelDeliveryPlatform / Win32 → pointer window in the source root
```

The decision service bypasses an event when smoothing is disabled, the target is automatically excluded full screen, a rule disables smoothing, or selected-applications-only mode has no matching enabled rule. Otherwise, the engine receives a settings snapshot and emits gradual deltas.

Before every inertial delta, the delivery adapter confirms that the pointer is still inside the root window captured from the original event. It then posts the appropriate vertical or horizontal wheel message directly to the window under the pointer, preserving the current modifier and mouse-button state. If validation or delivery fails, the engine cancels the remaining motion.

## Settings flow

```text
WinUI control → ViewModel → AppSettings → JsonSettingsService
                              ↓
                ScrollConfigurationSnapshotFactory → ScrollConfigurationStore
```

An executable-path rule wins over a process-name rule. The runtime engine consumes an immutable snapshot rather than the mutable settings object being edited by the UI.

## UI structure

- `Pages/` contains primary application pages.
- `Features/` groups scenarios with their ViewModels and controls.
- `Widgets/` contains larger reusable UI blocks.
- `Shared/` contains smaller presentation controls, converters, and helpers.
- `Composition/AppBootstrapper.cs` owns startup, tray integration, and resource cleanup.

## See Also

- [Getting Started](GETTING-STARTED.md) — build and first run.
- [Configuration](CONFIGURATION.md) — data moving through the layers.
- [Development](DEVELOPMENT.md) — conventions for changes.
