# API Reference

## RopResult

Каждый шаг pipeline возвращает `RopResult` или `RopResult<TData>` — обёртку над `Result` и актуальным `CancellationToken`.

```csharp
public sealed record RopResult<TData>(Result<TData> Result, CancellationToken Token);
public sealed record RopResult(Result Result, CancellationToken Token);
```

Неявное преобразование в `Result` / `Result<T>` позволяет писать:

```csharp
Result<int> result = await Do(() => 42);
```

### Вспомогательные методы

- `ToUntyped<T>()` — `RopResult<T>` → `RopResult`
- `FromUntyped<T>()` — `RopResult` → `RopResult<T>`
- `TryGetCallData(...)` — извлечение `ParametrizedError` из списка ошибок
- `WhenCallData(label, action)` — условный обработчик по метке шага
- `IsPipelineTerminated()` — проверка `PipelineTerminatedError`

## Do — первый шаг

### Без входных данных

```csharp
await Do(() => 42);
await Do(() => Result.Ok(42));
await Do(async () => await FetchAsync());
await Do(() => { /* side effect */ });
```

### С входными данными

```csharp
await Do(input, x => Transform(x));
await Do(input, x => Result.Ok(Transform(x)));
await Do(input);  // identity — пробрасывает вход
```

### DoEach — коллекция

```csharp
await DoEach(items, x => Process(x));
await DoEach(items, x => Result.Ok(Process(x)));
```

`null` и пустые коллекции на `Do` / `DoEach` → `NoDataError`.

Параметр `label` и `cancellationToken` доступны во всех перегрузках.

## Next — следующий шаг

Выполняется только при успехе предыдущего шага.

```csharp
await Do(() => items)
    .Next(xs => xs.Count)
    .Next(count => count * 2);
```

Side-effect без смены типа:

```csharp
await Do(() => order)
    .Next(o => SaveAsync(o));  // RopResult (untyped)
```

После untyped-шага:

```csharp
await Do(() => { })
    .Next(() => "hello");  // RopResult<string>
```

## Peek — наблюдение без изменения значения

```csharp
await Do(() => order)
    .Peek(o => Log(o.Id))
    .Next(o => Process(o));
```

При `Result.Fail` в `Peek` pipeline прерывается, значение не меняется.

## NextEach / PeekEach

```csharp
await Do(() => documents)
    .NextEach(doc => SendAsync(doc));           // side-effect

await Do(() => documents)
    .NextEach(doc => Result.Ok(doc.Id));       // IEnumerable<int>

await Do(() => documents)
    .PeekEach(doc => Log(doc.Id));             // коллекция без изменений
```

Поддерживаются `IEnumerable<T>`, `IReadOnlyCollection<T>`, `ICollection<T>`.

При сбое на элементе — `SequenceAbortedError<T>` с `AbortedOn` и `Reasons`.

## IfNoData

Только для коллекций. Вызывается при успешном шаге с пустой (но не `null`) коллекцией.

```csharp
// Мягкий пропуск — pipeline продолжается с пустой коллекцией
.IfNoData(_ => { })

// Подстановка данных
.IfNoData(_ => FetchDefaultsAsync())

// Намеренное завершение
.IfNoData(ctx => ctx.Terminate())  // → PipelineTerminatedError
```

## OnFailure — завершение pipeline

Все ошибки конкретного pipeline обрабатываются здесь. Итоговый `Result` содержит только `HandledFailureError`.

```csharp
Result<T> final = await pipeline
    .OnFailure(async (failed, token) =>
    {
        await LogAsync(failed, token);
    });
```

Перегрузки: `Action`, `Func<Task>`, `Func<CancellationToken, Task>`.

## CancellationToken

- Передаётся в `Do` через параметр `cancellationToken`
- Пробрасывается в `RopResult.Token` на каждом шаге
- Отменённый токен → `CancelledError`
- `OperationCanceledException` перехватывается и преобразуется в `CancelledError`

## Исключения

Необработанные исключения оборачиваются в `ExceptionalError` (FluentResults) и не пробрасываются дальше по pipeline.
