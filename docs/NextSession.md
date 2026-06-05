# HandleScope: handoff для следующей сессии

## Текущее состояние проекта

HandleScope сейчас находится после завершения Phase 1. Это WPF-приложение на .NET 9, созданное из стандартного шаблона и расширенное базовой архитектурой для дальнейшей разработки.

Репозиторий находится в `D:\Projects\HandleScope`.

Основной проект:

```text
D:\Projects\HandleScope\HandleScope\HandleScope.csproj
```

Целевая платформа:

```text
net9.0-windows
```

Тип приложения:

```text
WPF WinExe
```

Проект успешно собирался командой:

```powershell
dotnet build HandleScope.sln
```

На момент проверки сборка проходила без предупреждений и ошибок.

Git-состояние до коммита: проект и `.gitignore` были untracked, потому что репозиторий был только что создан/инициализирован. Перед продолжением стоит проверить:

```powershell
git status --short
```

## Что уже реализовано

### Базовая структура

Добавлена архитектурная структура внутри WPF-проекта:

```text
Application/
  Services/

Domain/
  Enums/
  Models/
  Results/

Infrastructure/
  Native/
  Process/

Presentation/
  Themes/
  ViewModels/
```

Пока проект остается одним WPF-проектом с папками. Разносить его на несколько проектов пока не требуется, но границы уже заложены так, чтобы это можно было сделать позже.

### NuGet-пакеты

В `HandleScope.csproj` добавлены:

```text
CommunityToolkit.Mvvm
Microsoft.Extensions.DependencyInjection
Serilog
Serilog.Sinks.Debug
Serilog.Sinks.File
```

### MVVM

Добавлен `MainWindowViewModel`:

```text
Presentation/ViewModels/MainWindowViewModel.cs
```

Он использует `CommunityToolkit.Mvvm`:

- `ObservableObject`
- `[ObservableProperty]`
- `[RelayCommand]`

Сейчас ViewModel содержит:

- `TargetPath`
- `IncludeChildren`
- `IsSearching`
- `StatusMessage`
- `SearchCommand`
- `CancelSearchCommand`

Поиск пока использует placeholder-сервис и не выполняет реальное обнаружение handles.

### DI и запуск приложения

`App.xaml` больше не использует `StartupUri`. Главное окно создается через DI в `App.xaml.cs`.

В `App.xaml.cs` настроены:

- `ServiceCollection`
- регистрация сервисов
- регистрация `MainWindowViewModel`
- регистрация `MainWindow`
- создание `ServiceProvider`
- открытие главного окна через `GetRequiredService<MainWindow>()`

### Serilog

Serilog настроен в `App.xaml.cs`.

Синки:

- Debug output
- rolling file log

Файлы логов пишутся в:

```text
logs/handlescope-.log
```

Настройки:

- minimum level: `Debug`
- rolling interval: day
- retention: 14 файлов

Логируются:

- старт приложения
- завершение приложения
- placeholder-вызов поиска
- ошибки поиска во ViewModel
- ошибки получения process details

### Доменные модели и контракты

Добавлены доменные типы:

```text
Domain/Enums/HandleSearchMode.cs
Domain/Models/HandleLockInfo.cs
Domain/Models/HandleSearchRequest.cs
Domain/Models/ProcessDetails.cs
Domain/Results/HandleSearchResult.cs
Domain/Results/ProcessActionResult.cs
```

Добавлены application-level интерфейсы:

```text
Application/Services/IHandleSearchService.cs
Application/Services/IProcessInfoService.cs
Application/Services/IProcessActionService.cs
```

`IProcessActionService` добавлен заранее для будущей возможности закрывать процесс, но сейчас содержит только placeholder-реализацию.

### Infrastructure placeholder-сервисы

Добавлены:

```text
Infrastructure/Native/PlaceholderHandleSearchService.cs
Infrastructure/Process/SystemProcessInfoService.cs
Infrastructure/Process/PlaceholderProcessActionService.cs
```

`PlaceholderHandleSearchService` возвращает пустой результат и warning о том, что native handle detection будет реализован позже.

`SystemProcessInfoService` уже умеет частично получать:

- PID
- имя процесса
- путь к executable, если доступен

Пользователь процесса пока не определяется.

`PlaceholderProcessActionService` возвращает сообщение, что закрытие процесса будет реализовано позже.

### UI

`MainWindow.xaml` заменен с пустого template-окна на базовый современный shell.

Сейчас окно содержит:

- заголовок `HandleScope`
- краткое описание
- поле ввода пути
- кнопку `Search`
- кнопку `Cancel`
- пустой `DataGrid`
- status area

Темы и стили находятся в:

```text
Presentation/Themes/Colors.xaml
Presentation/Themes/Controls.xaml
```

Используются кастомные WPF styles, без стороннего UI-фреймворка. Это намеренное решение для Phase 1: не добавлять тяжелую визуальную зависимость до стабилизации UX.

### Repo hygiene

Добавлен `.gitignore` в корень:

```text
.gitignore
```

Он исключает:

- `bin/`
- `obj/`
- `.vs/`
- user-файлы Visual Studio
- test/coverage artifacts
- logs

## Принятые архитектурные решения

### 1. WPF + .NET 9

Проект остается WPF-приложением на `net9.0-windows`.

### 2. MVVM через CommunityToolkit.Mvvm

ViewModels должны строиться через `CommunityToolkit.Mvvm`. Не нужно писать boilerplate `INotifyPropertyChanged` вручную.

Команды должны создаваться через `[RelayCommand]`.

### 3. Dependency Injection с Microsoft.Extensions.DependencyInjection

Даже для WPF используется DI-контейнер. Все сервисы и ViewModels должны создаваться через DI, а не через ручной `new` в UI-коде.

### 4. Serilog как основной логгер

Для логирования использовать Serilog. Не добавлять `Microsoft.Extensions.Logging` как основной logging facade без необходимости.

Сервисы сейчас получают `Serilog.ILogger` напрямую.

### 5. Четкие слои, даже внутри одного проекта

Текущие границы:

- `Presentation`: WPF views, ViewModels, themes, converters
- `Application`: интерфейсы сервисов, use cases, orchestration
- `Domain`: модели, enums, results, бизнес-структуры
- `Infrastructure`: Windows/native/process-specific реализации

Presentation не должен напрямую обращаться к native interop.

Native API и process-specific код должны жить в `Infrastructure`.

### 6. Реальное обнаружение locking-процессов будет native-based

Планируемый механизм для будущего этапа:

- `NtQuerySystemInformation`
- enumeration system handles
- `DuplicateHandle`
- `NtQueryObject`
- resolve object names
- normalize Windows device paths to drive paths

Restart Manager можно рассмотреть позже как дополнительную стратегию, но не как единственный источник.

### 7. Export functionality исключена

Экспорт результатов не нужен. Не добавлять CSV/JSON export.

Основной scope:

- найти процессы, блокирующие файл или папку
- показать process details
- refresh results
- позже добавить возможность закрыть процесс

### 8. Будущая функция: закрытие процесса

Будущая возможность должна быть сформулирована как close/terminate process, а не low-level unlock handle.

Предпочтительный будущий flow:

1. попытаться graceful close через `CloseMainWindow`
2. подождать
3. refresh results
4. если процесс все еще блокирует путь, предложить force terminate
5. для force terminate требовать отдельное подтверждение

Все попытки закрытия процесса должны логироваться.

## Подробный план следующего этапа: Phase 2

Цель Phase 2: сделать полноценный UI workflow поиска с mock/placeholder results, без native handle detection.

Phase 2 должен подготовить Presentation и ViewModel к реальным данным, которые появятся в Phase 3.

### Ожидаемый результат Phase 2

После Phase 2 приложение должно:

- принимать путь к файлу или папке
- позволять выбрать режим поиска: file или directory
- позволять включить/выключить recursive folder matching через `IncludeChildren`
- запускать поиск
- отменять поиск
- обновлять текущий поиск через Refresh
- показывать результаты в таблице
- показывать empty/loading/error states
- отображать warning-и из `HandleSearchResult`
- логировать действия пользователя и ошибки

Native detection все еще может быть placeholder/mock, но UI и ViewModel должны работать так, будто реальные результаты уже есть.

## Конкретные задачи на следующую сессию

### 1. Проверить текущее состояние

В начале следующей сессии выполнить:

```powershell
git status --short
dotnet build HandleScope.sln
```

Рабочая директория для build:

```text
D:\Projects\HandleScope\HandleScope
```

### 2. Добавить коллекцию результатов во ViewModel

В `MainWindowViewModel` добавить observable collection для результатов:

```text
ObservableCollection<HandleLockInfo> Results
```

Или отдельный presentation model, если нужно отделить domain model от grid:

```text
HandleLockResultViewModel
```

Для Phase 2 допустимо использовать `HandleLockInfo` напрямую, но лучше сразу создать отдельную row ViewModel, если начнется форматирование UI-полей.

### 3. Привязать DataGrid к результатам

В `MainWindow.xaml` у `DataGrid` добавить:

```text
ItemsSource="{Binding Results}"
```

Проверить названия колонок:

- `ProcessName`
- `ProcessId`
- `UserName`
- `ProcessPath`
- `LockedPath`
- `HandleType`
- `Access`

Сейчас `DataGrid` уже имеет часть колонок, но не привязан к коллекции.

### 4. Добавить SearchMode в UI и ViewModel

Сейчас `MainWindowViewModel` всегда создает:

```csharp
HandleSearchMode.File
```

Нужно добавить состояние:

```text
SelectedSearchMode
```

UI должен позволять выбрать:

- File
- Folder

Можно начать с `ComboBox` или двух radio buttons. Позже это можно заменить на segmented control.

### 5. Добавить IncludeChildren toggle

`IncludeChildren` уже есть во ViewModel, но не выведен в UI.

Добавить checkbox/toggle:

```text
Include child paths
```

Показывать или активировать его главным образом для folder mode.

### 6. Добавить RefreshCommand

Refresh должен повторять последний успешно заданный search request.

Рекомендуемая модель:

```text
private HandleSearchRequest? _lastSearchRequest;
```

При успешном старте Search сохранить request.

`RefreshCommand`:

- disabled, если `_lastSearchRequest` отсутствует
- disabled во время поиска
- запускает тот же request повторно

### 7. Улучшить SearchCommand

Текущая команда только вызывает placeholder service и обновляет status.

Нужно:

- очищать предыдущие результаты перед новым поиском
- заполнять `Results`
- сохранять warnings
- показывать количество найденных записей
- корректно обрабатывать cancellation
- логировать target path, mode, includeChildren

### 8. Добавить состояние warnings/errors

Добавить во ViewModel:

```text
ObservableCollection<string> Warnings
string? ErrorMessage
bool HasResults
bool HasWarnings
bool HasError
```

В UI можно вывести компактную warning/status область. Не делать сложную систему уведомлений на этом этапе.

### 9. Сделать placeholder-сервис полезнее для UI

Для Phase 2 стоит временно изменить `PlaceholderHandleSearchService`, чтобы он возвращал mock results, если путь не пустой.

Пример данных:

- PID текущего процесса
- process name `HandleScope`
- locked path равен target path
- handle type `File`
- access `Read`

Это позволит проверить bindings, таблицу и refresh без native interop.

Важно: явно логировать, что это mock/placeholder result.

### 10. Добавить Browse-кнопку

Добавить кнопку рядом с path input.

Для выбора файла можно использовать стандартный dialog. Для выбора папки в WPF можно использовать:

```text
Microsoft.Win32.OpenFileDialog
```

для файлов, а для папок лучше добавить отдельную реализацию через Windows Forms `FolderBrowserDialog` или современный Common Item Dialog позже.

Для Phase 2 можно начать с file browse и оставить folder browse как TODO, либо добавить оба режима.

Если используется `System.Windows.Forms.FolderBrowserDialog`, нужно проверить project settings/package requirements.

### 11. Проверить UI на базовую адаптивность

Минимальная проверка:

- окно не ломается на `MinWidth=860`
- кнопки не перекрывают path input
- status text не перекрывает таблицу
- DataGrid занимает основное пространство

### 12. Собрать проект

После изменений обязательно выполнить:

```powershell
dotnet build HandleScope.sln
```

Если добавлялись новые пакеты, убедиться, что restore прошел успешно.

## Важные ограничения

Не реализовывать в Phase 2:

- native handle enumeration
- `NtQuerySystemInformation`
- `DuplicateHandle`
- `NtQueryObject`
- закрытие процесса
- force terminate
- экспорт результатов

Phase 2 должен быть UI/application workflow этапом.

## Рекомендуемые критерии готовности Phase 2

Phase 2 можно считать завершенным, если:

- приложение собирается без warnings/errors
- путь вводится и сохраняется во ViewModel
- можно выбрать file/folder mode
- можно включить/выключить include children
- search заполняет таблицу mock/placeholder результатами
- cancel не ломает состояние
- refresh повторяет последний search
- warnings отображаются
- ошибки логируются через Serilog
- UI остается аккуратным и понятным

## Следующий этап после Phase 2

Phase 3 должен начать реальную native handle detection реализацию.

Ориентировочные компоненты Phase 3:

```text
Infrastructure/Native/NativeMethods.cs
Infrastructure/Native/SystemHandleEnumerator.cs
Infrastructure/Native/ObjectNameResolver.cs
Infrastructure/Native/DevicePathMapper.cs
Infrastructure/Native/NativeHandleSearchService.cs
```

На Phase 3 также стоит добавить unit tests для path normalization, потому что корректное сравнение путей будет критичным для поиска по файлам и папкам.
