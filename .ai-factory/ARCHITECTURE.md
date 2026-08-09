# Архитектура: явная архитектура с техническими слоями

## Обзор

SmoothScroll использует четыре чётко разделённых проекта. Такая структура сохраняет физику прокрутки и правила приложений независимыми от WinUI и Windows API, а платформенные детали остаются заменяемыми адаптерами.

Практический приоритет — безопасная работа с глобальным вводом и быстрый путь события от хука до целевого окна без смешивания UI, бизнес-решений и P/Invoke.

## Основания решения

- **Тип проекта:** настольное Windows-приложение с глобальным вводом и настройками пользователя.
- **Стек:** C#, .NET 10, WinUI 3, SharpHook, Win32, JSON и xUnit.
- **Ключевой фактор:** нужно тестировать решение о прокрутке и физику без запуска UI, хука или реальной доставки мыши.

## Структура

```text
SmoothScrollModern.Domain/
├── Settings/                 # AppSettings, профили, правила и их валидация
└── Scroll/                   # перечисления режима доставки

SmoothScrollModern.Application/
├── Scroll/                   # движок, решения и неизменяемые снимки конфигурации
├── Applications/             # контракты и правила приложений
├── Input/                    # контракты глобального ввода и доставки событий
├── Settings/, Startup/, Tray/ # контракты внешних сервисов
└── Core/                     # общие константы приложения

SmoothScrollModern.Infrastructure/
├── Input/, Native/           # SharpHook, Win32 и отправка колеса
├── Applications/             # активное окно и идентификация процесса
├── Settings/                 # JSON-настройки
├── Startup/, Tray/           # интеграции Windows
└── Properties/               # assembly metadata

SmoothScrollModern/
├── Composition/              # AppBootstrapper
├── Features/, Widgets/       # ViewModel и составные WinUI-контролы
├── Pages/                    # страницы оболочки
├── Shared/                   # общие presentation-компоненты
└── App.xaml.cs               # composition root
```

## Правила зависимостей

- ✅ `SmoothScrollModern` может ссылаться на Application, Domain и Infrastructure.
- ✅ Infrastructure может реализовывать контракты Application и использовать модели Domain.
- ✅ Application может ссылаться на Domain.
- ❌ Domain не ссылается на другие проекты решения.
- ❌ Application не импортирует WinUI, SharpHook, P/Invoke, `System.Windows.Forms` или `System.IO` для работы с пользовательским хранилищем.
- ❌ Infrastructure и Domain не зависят от ViewModel, страниц или XAML.

## Связь слоёв

1. `App.xaml.cs` собирает DI-контейнер, сопоставляя интерфейсы Application с адаптерами Infrastructure.
2. `AppBootstrapper` управляет жизненным циклом: запускает трей, подключает глобальный ввод и обновляет UI.
3. `SharpHookInputService` передаёт событие колеса в Application; `ScrollDecisionService` выбирает обход или профиль.
4. `SmoothScrollEngine` рассчитывает шаги инерции и вызывает `IInputInjectionService`; Infrastructure доставляет их через Windows API.
5. Изменение UI обновляет `AppSettings`, сохраняет JSON и публикует неизменяемый снимок для пути ввода.

## Ключевые принципы

1. Доменные настройки проверяют собственные инварианты до попадания в runtime-снимки.
2. Внешние зависимости выражаются интерфейсами Application и реализуются в Infrastructure.
3. ViewModel координирует представление, но не содержит P/Invoke, файловый IO или физику прокрутки.
4. Снимки конфигурации делают путь глобального события стабильным и независимым от редактирования UI.
5. Новые сценарии расширяют слой по ответственности, а не обходят существующие границы.

## Организация существующего кода

- **Новые возможности:** следуют этим границам без исключений.
- **Существующий код:** сохраняет текущую структуру; не рефакторите несвязанные части только ради косметического выравнивания.
- **Взаимодействие:** изолируйте пересечение новых и старых частей интерфейсом, адаптером или небольшим фасадом.

## Примеры

### Контракт во внутреннем слое и адаптер во внешнем

```csharp
// Application
public interface IActiveWindowService
{
    ApplicationInfo GetActiveApplication();
}

// Infrastructure
public sealed class ActiveWindowService : IActiveWindowService
{
    // Получает данные активного окна через изолированный Windows API.
}
```

### Регистрация только в composition root

```csharp
// SmoothScrollModern/App.xaml.cs
services.AddSingleton<IActiveWindowService, ActiveWindowService>();
services.AddSingleton<IScrollDecisionService, ScrollDecisionService>();
```

## Антипаттерны

- ❌ Вызов `NativeMethods` из ViewModel или Domain.
- ❌ Сохранение JSON напрямую из XAML-контрола.
- ❌ Получение активного окна внутри `SmoothScrollEngine` вместо использования контракта и решения Application.
- ❌ Передача изменяемого `AppSettings` в фоновую физику вместо снимка конфигурации.
