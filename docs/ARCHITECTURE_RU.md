[← Начало работы](GETTING-STARTED_RU.md) · [Назад к README](README_RU.md) · [Конфигурация →](CONFIGURATION_RU.md) · [English](ARCHITECTURE.md)

# Архитектура приложения

## Слои

| Слой | Проект | Ответственность |
|---|---|---|
| Domain | `SmoothScrollModern.Domain` | Модели настроек, профили, правила и валидация |
| Application | `SmoothScrollModern.Application` | Решения о прокрутке, физика, снимки и контракты |
| Infrastructure | `SmoothScrollModern.Infrastructure` | SharpHook, Win32, JSON, трей и автозапуск |
| Presentation | `SmoothScrollModern` | WinUI 3, страницы, ViewModel и DI |

Направление зависимостей: `Presentation → Application → Domain`; Infrastructure реализует контракты Application и также использует Domain. Подробные правила для разработки доступны в [архитектурном контексте](../.ai-factory/ARCHITECTURE.md).

## Поток прокрутки

```text
Событие колеса → SharpHookInputService → ScrollDecisionService
→ SmoothScrollEngine → IInputInjectionService → WindowsWheelDeliveryPlatform / Win32 → окно под курсором в исходном root
```

`ScrollDecisionService` пропускает событие, когда сглаживание выключено, приложение полноэкранное и автоматически исключено, правило запрещает сглаживание либо активен режим «только выбранные приложения» без подходящего правила. Иначе движок получает снимок параметров и постепенно выдаёт дельты.

Перед каждой инерционной дельтой адаптер доставки убеждается, что курсор остаётся в root-окне, захваченном исходным событием. Затем он адресно отправляет вертикальное или горизонтальное wheel-сообщение в окно под курсором, сохраняя текущее состояние модификаторов и кнопок мыши. Если проверка или доставка не удалась, движок отменяет остаток движения.

## Поток настроек

```text
WinUI control → ViewModel → AppSettings → JsonSettingsService
                              ↓
                ScrollConfigurationSnapshotFactory → ScrollConfigurationStore
```

Путь по полному имени исполняемого файла имеет приоритет над правилом по имени процесса. Runtime-движок использует неизменяемый снимок, а не редактируемый объект настроек.

## Структура UI

- `Pages/` содержит основные страницы приложения.
- `Features/` группирует сценарии и их ViewModel/контролы.
- `Widgets/` объединяет крупные переиспользуемые блоки интерфейса.
- `Shared/` содержит небольшие общие контролы, конвертеры и helpers.
- `Composition/AppBootstrapper.cs` управляет запуском, системным треем и очисткой ресурсов.

## См. также

- [Начало работы](GETTING-STARTED_RU.md) — сборка и запуск.
- [Конфигурация](CONFIGURATION_RU.md) — данные, проходящие через слои.
- [Разработка](DEVELOPMENT_RU.md) — соглашения для изменений.
