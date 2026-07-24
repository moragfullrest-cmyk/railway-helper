# План: абстрагирование от FluentResults

## Цель

Сделать ядро railway-pipeline независимым от FluentResults. Своя модель результата (`Outcome`) — внутренняя и публичная валюта цепочки; FluentResults остаётся conversion-слоем на границе (вход шагов / выход `OnFailure`), а не несущей шиной.

## Текущее состояние

FluentResults — прямая зависимость (`3.16.0`). Связь тотальная:

- публичный API: `Result`, `Result<T>`, `ResultBase`, `Error`, `IError` в сигнатурах;
- ошибки библиотеки наследуют `FluentResults.Error`;
- `RopResult` / `RopResult<T>` оборачивают `Result` / `Result<T>`;
- внутренний канон после `Lift`: `ValueTask<Result>` / `ValueTask<Result<T>>`;
- `Fail*`, `AttachContext`, `OnFailure`, `Each` работают с API FluentResults.

Отдельного Result-слоя нет. Точка врезки уже есть: `Lift` нормализует делегаты, `RopResult` — носитель шага.

## Рекомендуемый подход

**Свой `Outcome` внутри, FluentResults — на границе.**

Не вводить DI/`IResultEngine` «под любую Result-библиотеку». Достаточно своего value-type Result + тонких conversion на краю.

### Модель ядра

```csharp
public readonly struct Outcome
{
    public bool IsSuccess { get; }
    public IReadOnlyList<IFailure> Failures { get; }
    public static Outcome Ok();
    public static Outcome Fail(params IFailure[] failures);
}

public readonly struct Outcome<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public IReadOnlyList<IFailure> Failures { get; }
    public static Outcome<T> Ok(T value);
    public static Outcome<T> Fail(params IFailure[] failures);
}

public interface IFailure
{
    string Message { get; }
    IReadOnlyList<IFailure> Causes { get; }
}
```

- `RopResult` / `RopResult<T>` несут `Outcome` / `Outcome<T>`.
- `CancelledError`, `NoDataError`, `SequenceAbortedError<T>`, `PipelineTerminatedError`, `HandledFailureError`, `ParametrizedError` наследуют свой `Failure`, не `FluentResults.Error`.

### Точка подмены: `Lift` + `Fail*`

| Слой | Было | Станет |
|------|------|--------|
| `Lift` | `Result.Ok` / pass-through `Result` | `Outcome.Ok` / map `Result` → `Outcome` |
| `FailCancelled` и др. | `Result.Fail(...)` | `Outcome.Fail(new CancelledError())` |
| `AttachContext` | `IError` + `ParametrizedError` | `IFailure` + свой `ParametrizedError` |
| `Do` / `Next` / `Peek` / `Each` | `IsFailed` / `Errors` / `Value` | то же у `Outcome` |

Публичные перегрузки `Func<Result<T>>` остаются через адаптер в `Lift`, без протекания FluentResults внутрь pipeline.

## Граница совместимости

**Вариант A (мягкий, один пакет):** ядро на `Outcome`, в `RailwayHelper` остаются overloads / `implicit` для FluentResults.

**Вариант B (чистый, major):**

- `RailwayHelper` — без зависимости от FluentResults;
- `RailwayHelper.FluentResults` — `ToOutcome()` / `ToResult()`, overloads `Do(() => Result.Ok(...))`, `OnFailure` → `Result`.

Практичный путь: **A сейчас → B в мажоре**.

### Скелет адаптера

```csharp
// Core — без FluentResults
public readonly struct RopResult<T>(Outcome<T> outcome, CancellationToken token)
{
    public Outcome<T> Outcome { get; } = outcome;
    public CancellationToken Token { get; } = token;
}

// FluentResults bridge (тот же или соседний пакет)
public static class FluentResultsBridge
{
    public static Outcome<T> ToOutcome<T>(this Result<T> r) =>
        r.IsSuccess ? Outcome.Ok(r.Value) : Outcome.Fail(MapErrors(r.Errors));

    public static Result<T> ToResult<T>(this Outcome<T> o) =>
        o.IsSuccess ? Result.Ok(o.Value) : Result.Fail<T>(MapFailures(o.Failures));
}
```

`Lift.From(Func<..., Result<T>>)` сразу делает `ToOutcome()` — дальше pipeline FluentResults не видит.

## Этапы миграции

1. Ввести `Outcome` / `IFailure`; перевести `Internal.*` на них.
2. `RopResult` → обёртка над `Outcome`; dual conversion `Outcome` ↔ `Result` (пока FluentResults в зависимостях).
3. `OnFailure` принимать свой тип сбоев; overload на `ResultBase` — obsolete.
4. В major: убрать FluentResults из core, вынести адаптер в отдельный пакет.

### Порядок правок по файлам

1. `RailwayHelper.Core.cs` — типы `Outcome` / `IFailure` / свои ошибки; новый носитель в `RopResult`.
2. `RailwayHelper.Internal.Lift.cs` — канон `ValueTask<Outcome[T]>`, map с `Result`.
3. `RailwayHelper.Internal.Pipeline.cs` — `Fail*`, `AttachContext`, exceptional path.
4. `Internal.Do` / `Next` / `Peek` / `Each` / `Collections` / `IfNoData` — чтение `Outcome`.
5. Публичные `Do` / `Next` / `Peek` / `IfNoData` / `OnFailure` — dual overloads, затем deprecation.
6. Документация (`README`, `docs/api.md`, `docs/examples.md`) и тесты/samples.

## Что не делать

- Facade `IResult` вокруг `FluentResults.Result` без своей модели — зависимость остаётся.
- Полный отказ от Result в пользу exceptions / произвольных DU — ломает ROP и DX.
- Source generator / multi-backend — избыточно для одного пакета.

## Альтернативы (отклонённые как основной путь)

| Подход | Когда уместен |
|--------|----------------|
| Только facade над FluentResults | Нужна смена API «на бумаге», зависимость остаётся |
| Exceptions + discriminated union | Ломает текущий ROP DX |
| DI/`IResultEngine` под несколько библиотек | Избыточная сложность |

## Вердикт

Абстрагировать не интерфейсом над FluentResults, а своим `Outcome` как валютой pipeline. FluentResults — conversion на входе шагов и на выходе `OnFailure`. Точка врезки подготовлена: `Lift` + `RopResult`.
