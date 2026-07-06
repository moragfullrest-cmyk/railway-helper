# RailwayHelper

**Railway-oriented pipeline** поверх [FluentResults](https://github.com/altmann/FluentResults) для C#.

Цепочки начинаются с `Do`, продолжаются через `Next`, `Peek`, `NextEach`, `PeekEach`, завершаются обработчиком `OnFailure`.

## Установка

```bash
dotnet add package RailwayHelper
```

## Быстрый старт

```csharp
using FluentResults;
using RailwayHelper;
using static RailwayHelper.RailwayHelper;

Result<int> result = await Do(() => LoadItemsAsync())
    .Next(items => items.Sum(x => x.Amount))
    .OnFailure(LogPipelineError);

if (result.IsSuccess)
    Console.WriteLine($"Total: {result.Value}");
```

## Основные операции

| Метод | Назначение |
|-------|------------|
| `Do` | Первый шаг pipeline (с входом, без входа, side-effect, `DoEach`) |
| `Next` | Преобразование или side-effect над значением предыдущего шага |
| `Peek` | Side-effect без изменения значения |
| `NextEach` / `PeekEach` | Последовательная обработка коллекции |
| `IfNoData` | Реакция на пустую коллекцию (продолжить, подставить данные, `Terminate`) |
| `OnFailure` | Централизованная обработка ошибок pipeline |

## Метки шагов и контекст ошибок

При сбое шага с `label` к ошибкам добавляется `ParametrizedError` — входные данные и метка для маршрутизации в `OnFailure`:

```csharp
const string LoadLabel = "load";

await Do(orderId, id => LoadOrderAsync(id), label: LoadLabel)
    .Next(order => ProcessOrderAsync(order))
    .OnFailure(result =>
    {
        result.WhenCallData<int>(LoadLabel, (id, _) => LogError($"Order {id} failed"));
    });
```

## Обработка пустых данных

- `null` и пустые коллекции (кроме `string`) на `Do` / `DoEach` → `NoDataError`
- Пустые коллекции на `Next` / `NextEach` обрабатываются штатно
- Осознанная реакция на пустоту — через `IfNoData`:

```csharp
await Do(() => FetchDocumentsAsync())
    .IfNoData(ctx => ctx.Terminate())   // мягкое завершение
    .Next(docs => SendReportsAsync(docs));
```

## Типы ошибок

| Тип | Когда |
|-----|-------|
| `CancelledError` | Отмена по `CancellationToken` |
| `NoDataError` | `null` или пустая коллекция на входе `Do` |
| `SequenceAbortedError<T>` | Сбой при обработке элемента в `DoEach` / `NextEach` |
| `PipelineTerminatedError` | Вызов `NoDataContext.Terminate()` в `IfNoData` |
| `HandledFailureError` | Итог после `OnFailure` (исходные ошибки не пробрасываются) |
| `ExceptionalError` | Необработанное исключение (из FluentResults) |

## Сборка и публикация

```bash
cd railway-helper

# Сборка
dotnet build RailwayHelper.sln -c Release

# Тесты
dotnet test RailwayHelper.sln -c Release

# Пример
dotnet run --project samples/RailwayHelper.Samples/RailwayHelper.Samples.csproj

# NuGet-пакет
dotnet pack src/RailwayHelper/RailwayHelper.csproj -c Release -o ./artifacts

# Публикация (после настройки API key)
dotnet nuget push ./artifacts/RailwayHelper.*.nupkg --source https://api.nuget.org/v3/index.json --api-key <YOUR_API_KEY>
```

## Документация

- [Руководство по API](docs/api.md) — подробное описание операций и перегрузок
- [Примеры](docs/examples.md) — типовые сценарии
- [Публикация в NuGet](docs/publishing.md) — сборка, pack и push
- XML-документация генерируется при сборке (`GenerateDocumentationFile`)

## Лицензия

MIT — см. [LICENSE](LICENSE).
