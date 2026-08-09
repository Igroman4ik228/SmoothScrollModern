[← Конфигурация](CONFIGURATION_RU.md) · [Назад к README](README_RU.md) · [Тестирование →](TESTING_RU.md) · [English](DEVELOPMENT.md)

# Разработка

## Рабочий цикл

```powershell
dotnet build SmoothScrollModern.slnx
dotnet test SmoothScrollModern.slnx
```

После изменений кода или файлов проекта сначала убедитесь, что сборка проходит без ошибок. Перед отправкой изменений запускайте тесты, если менялась логика Application, Infrastructure или модели настроек.

## Где размещать код

| Задача | Место |
|---|---|
| Новая модель настроек и её инварианты | `SmoothScrollModern.Domain` |
| Чистая логика, сценарий или контракт внешней зависимости | `SmoothScrollModern.Application` |
| Реальная интеграция с Windows, IO, SharpHook или Win32 | `SmoothScrollModern.Infrastructure` |
| ViewModel, XAML, команды и композиция интерфейса | `SmoothScrollModern` |
| Изолированная проверка поведения | `SmoothScrollModern.Application.Tests` |

Новая внешняя реализация начинается с контракта в Application, получает реализацию в Infrastructure и регистрируется в `App.xaml.cs`.

## UI

Следуйте Fluent/WinUI-паттернам: формируйте ясную иерархию, не перегружайте поверхности и сохраняйте XAML-контролы компактными. `SettingsCard` и `SettingsExpander` подходят только для привычных Windows-сценариев настроек.

ViewModel хранит команды и состояние. Code-behind допустим для небольшой UI-специфичной связки, но не для файлового IO, P/Invoke и бизнес-логики.

## Нативный код и ввод

- Сохраняйте P/Invoke в `Infrastructure/Native` и оборачивайте ошибки в понятные исключения или результаты.
- Не вызывайте Windows API из Domain, Application или XAML-контролов.
- Не удерживайте блокировки во время внешней доставки события колеса.
- Завершайте отменяемые и нативные ресурсы через жизненный цикл `AppBootstrapper`.

## См. также

- [Тестирование](TESTING_RU.md) — проверка изменений.
- [Архитектура](ARCHITECTURE_RU.md) — подробные направления зависимостей.
- [Конфигурация](CONFIGURATION_RU.md) — правила изменения настроек.
