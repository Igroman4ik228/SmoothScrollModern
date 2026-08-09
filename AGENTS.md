# AGENTS.md

> Обновляйте эту карту при существенном изменении структуры решения или набора документации.

## Обзор проекта

SmoothScroll — настольное Windows-приложение на WinUI 3, которое перехватывает глобальную прокрутку мыши и доставляет сглаженные события в активное окно. Подробный контекст проекта — в [.ai-factory/DESCRIPTION.md](.ai-factory/DESCRIPTION.md).

## Технологический стек

- **Язык:** C# с nullable reference types и implicit usings.
- **Платформа:** .NET `net10.0-windows10.0.19041.0`, WinUI 3 и Windows App SDK.
- **UI и MVVM:** CommunityToolkit.Mvvm, CommunityToolkit.WinUI.Controls и WinUIEx.
- **Ввод и интеграция с Windows:** SharpHook, Win32 interop и Windows Forms для трея.
- **Тесты:** xUnit и Microsoft.NET.Test.Sdk.
- **Хранилище:** локальный JSON-файл; внешней базы данных нет.

## Структура проекта

```text
SmoothScrollModern/                   # WinUI 3 presentation layer и точка входа
├── App.xaml.cs                        # DI-контейнер и запуск приложения
├── Composition/                       # AppBootstrapper и связывание сервисов
├── Features/                          # ViewModel и UI-контролы по сценариям
├── Pages/                             # страницы настроек, профилей и исключений
├── Widgets/                           # композиционные UI-блоки
└── Shared/                            # общие контролы и presentation helpers
SmoothScrollModern.Domain/             # модели настроек и инварианты предметной области
SmoothScrollModern.Application/        # сценарии, интерфейсы и движок прокрутки
SmoothScrollModern.Infrastructure/     # SharpHook, Win32, JSON, трей и автозапуск
SmoothScrollModern.Application.Tests/  # unit-тесты Application и Infrastructure
docs/                                  # пользовательская и разработческая документация
.ai-factory/                           # контекст, архитектура и правила для агентов
```

## Ключевые точки входа

| Файл | Назначение |
|---|---|
| `SmoothScrollModern.slnx` | решение и целевые платформы x64, x86, ARM64 |
| `SmoothScrollModern/App.xaml.cs` | конфигурация DI и запуск `AppBootstrapper` |
| `SmoothScrollModern/Composition/AppBootstrapper.cs` | жизненный цикл, трей и глобальный ввод |
| `SmoothScrollModern.Application/Scroll/SmoothScrollEngine.cs` | физика и доставка сглаженной прокрутки |
| `SmoothScrollModern.Application/Scroll/ScrollDecisionService.cs` | решение о применении правила к окну |
| `SmoothScrollModern.Infrastructure/Settings/JsonSettingsService.cs` | загрузка, сохранение, импорт и экспорт настроек |

## Документация

| Документ | Путь | Описание |
|---|---|---|
| README | `docs/README.md` | Английский обзор и старт |
| README (RU) | `docs/README_RU.md` | Русский обзор и старт |
| Начало работы | `docs/GETTING-STARTED.md` | Требования и первый запуск |
| Начало работы (RU) | `docs/GETTING-STARTED_RU.md` | Русская версия руководства |
| Архитектура | `docs/ARCHITECTURE.md` | Слои и поток данных |
| Архитектура (RU) | `docs/ARCHITECTURE_RU.md` | Русская версия руководства |
| Конфигурация | `docs/CONFIGURATION.md` | Настройки и профили |
| Конфигурация (RU) | `docs/CONFIGURATION_RU.md` | Русская версия руководства |
| Разработка | `docs/DEVELOPMENT.md` | Структура и практики |
| Разработка (RU) | `docs/DEVELOPMENT_RU.md` | Русская версия руководства |
| Тестирование | `docs/TESTING.md` | Команды и покрытие |
| Тестирование (RU) | `docs/TESTING_RU.md` | Русская версия руководства |
| Диагностика | `docs/TROUBLESHOOTING.md` | Решение типовых проблем |
| Диагностика (RU) | `docs/TROUBLESHOOTING_RU.md` | Русская версия руководства |

## Контекст для ИИ

| Файл | Назначение |
|---|---|
| `.ai-factory/DESCRIPTION.md` | Цель проекта, стек и функциональность |
| `.ai-factory/ARCHITECTURE.md` | Архитектурные границы и направления зависимостей |
| `.ai-factory/RULES.md` | Обязательные краткие правила проекта |
| `.ai-factory/rules/base.md` | Детальные соглашения кода и модулей |

## Правила для агентов

- После любого изменения кода или проектного файла запускайте `dotnet build SmoothScrollModern.slnx` и сообщайте результат без ошибок.
- Соблюдайте зависимости `Presentation → Application → Domain` и `Infrastructure → Application/Domain`; не переносите WinUI, SharpHook или Win32-типы во внутренние слои.
- Регистрируйте новые реализации интерфейсов в композиционном корне `App.xaml.cs`, а не в ViewModel или доменных моделях.
- Для UI следуйте [Windows app design guidance](https://learn.microsoft.com/en-us/windows/apps/design/), ориентируйтесь на [WinUI Gallery](https://github.com/microsoft/WinUI-Gallery) и [CommunityToolkit for Windows](https://github.com/CommunityToolkit/Windows). Предпочитайте Fluent/WinUI-паттерны, ясную иерархию и сдержанные поверхности; `SettingsCard` и `SettingsExpander` применяйте только для уместных настроек Windows.
- Дополняйте или обновляйте unit-тесты при изменении детерминированной логики Application либо Infrastructure.
- Не объединяйте независимые команды оболочки через `&&`; запускайте их по отдельности, чтобы ошибки и результат каждой операции были видны.
- Храните пользовательскую документацию только в `docs/`: английские имена файлов пишите ВЕРХНИМ РЕГИСТРОМ без суффикса, а русские копии — с тем же именем и суффиксом `_RU`; обе версии должны быть равнозначны по содержанию.
