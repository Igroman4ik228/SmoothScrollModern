[← Development](DEVELOPMENT.md) · [Back to README](README.md) · [Troubleshooting →](TROUBLESHOOTING.md) · [Русский](TESTING_RU.md)

# Testing

## Run the suite

```powershell
dotnet test SmoothScrollModern.slnx
```

`SmoothScrollModern.Application.Tests` uses xUnit. The command restores dependencies, builds the solution, and runs the tests.

## Existing coverage

| Area | Example behavior |
|---|---|
| Scroll engine | Stops inertia when delivery is rejected and handles deltas |
| Scroll decision | Enablement, pause, full-screen state, and application rules |
| Configuration | Snapshot construction and publication |
| Application rules | Normalization, creation, and matching |
| Integration boundaries | `InputInjectionService` behavior through test doubles |

## Writing tests

- Isolate `SmoothScrollEngine`, `ScrollDecisionService`, and snapshot factories from real windows and hooks.
- Replace `IInputInjectionService`, `IWindowIdentityResolver`, and other adapters with simple test doubles.
- For asynchronous physics, await an observable event with a timeout instead of waiting indefinitely.
- Name tests after action, expected result, and condition, for example `EnqueueWheel_StopsInertiaWhenDeliveryIsRejected`.

## Manual checks

After UI or Windows-integration changes, launch the app and check startup, theme persistence, application rules, tray pause/exit, and scrolling in another application.

## See Also

- [Development](DEVELOPMENT.md) — workflow and test placement.
- [Troubleshooting](TROUBLESHOOTING.md) — build and runtime support.
- [Getting Started](GETTING-STARTED.md) — environment requirements.
