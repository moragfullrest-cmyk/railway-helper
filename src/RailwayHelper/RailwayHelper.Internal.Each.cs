using System.Collections;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Each

    /// <summary>Формирует итоговый результат итерации по коллекции с преобразованием.</summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Накопленные результаты.</param>
    /// <param name="abortedOn">Элемент, на котором итерация прервана.</param>
    /// <param name="iterationFailure">Ошибочный результат итерации.</param>
    /// <param name="token">Токен отмены.</param>
    private static Result<IEnumerable<TOutput>> EachCoreFinalize<TInput, TOutput>(
        IEnumerable<TOutput> items,
        TInput abortedOn,
        Result iterationFailure,
        CancellationToken token) =>
        (token.IsCancellationRequested, iterationFailure.IsFailed) switch
        {
            (true, _) => FailCancelled<IEnumerable<TOutput>>(token),
            (_, true) => FailSequenceAborted<IEnumerable<TOutput>, TInput>(abortedOn, iterationFailure),
            _ => Result.Ok<IEnumerable<TOutput>>(items),
        };

    /// <summary>Формирует итоговый результат итерации по коллекции с побочным эффектом.</summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="abortedOn">Элемент, на котором итерация прервана.</param>
    /// <param name="iterationFailure">Ошибочный результат итерации.</param>
    /// <param name="token">Токен отмены.</param>
    private static Result EachCoreVoidFinalize<TInput>(
        TInput abortedOn,
        Result iterationFailure,
        CancellationToken token) =>
        (token.IsCancellationRequested, iterationFailure.IsFailed) switch
        {
            (true, _) => FailCancelled(token),
            (_, true) => FailSequenceAborted(abortedOn, iterationFailure),
            _ => Result.Ok(),
        };

    /// <summary>
    /// Последовательно применяет шаг к каждому элементу коллекции и возвращает накопленные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static async ValueTask<Result<IEnumerable<TOutput>>> EachCore<TInput, TOutput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> step,
        CancellationToken token)
    {
        (IEnumerable<TOutput> items, TInput abortedOn, Result iterationFailure) = await SelectEachAsync<TInput, TOutput>(source, step, token);
        return EachCoreFinalize<TInput, TOutput>(items, abortedOn, iterationFailure, token);
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции и возвращает накопленные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result<IEnumerable<TOutput>>> EachCoreSync<TInput, TOutput>(
        IEnumerable<TInput> source,
        Func<TInput, TOutput> step,
        CancellationToken token)
    {
        (IEnumerable<TOutput> items, TInput abortedOn, Result iterationFailure) = SelectEachSync(source, step, token);
        return ValueTask.FromResult(EachCoreFinalize<TInput, TOutput>(items, abortedOn, iterationFailure, token));
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции и возвращает накопленные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result<IEnumerable<TOutput>>> EachCoreSync<TInput, TOutput>(
        IEnumerable<TInput> source,
        Func<TInput, Result<TOutput>> step,
        CancellationToken token)
    {
        (IEnumerable<TOutput> items, TInput abortedOn, Result iterationFailure) = SelectEachSync(source, step, token);
        return ValueTask.FromResult(EachCoreFinalize<TInput, TOutput>(items, abortedOn, iterationFailure, token));
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции и возвращает накопленные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result<IEnumerable<TOutput>>> EachCoreSync<TInput, TOutput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, TOutput> step,
        CancellationToken token)
    {
        (IEnumerable<TOutput> items, TInput abortedOn, Result iterationFailure) = SelectEachSync(source, step, token);
        return ValueTask.FromResult(EachCoreFinalize<TInput, TOutput>(items, abortedOn, iterationFailure, token));
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции и возвращает накопленные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result<IEnumerable<TOutput>>> EachCoreSync<TInput, TOutput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, Result<TOutput>> step,
        CancellationToken token)
    {
        (IEnumerable<TOutput> items, TInput abortedOn, Result iterationFailure) = SelectEachSync(source, step, token);
        return ValueTask.FromResult(EachCoreFinalize<TInput, TOutput>(items, abortedOn, iterationFailure, token));
    }

    /// <summary>
    /// Последовательно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static async ValueTask<Result> EachCoreVoid<TInput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        CancellationToken token)
    {
        TInput abortedOn = default;
        Result iterationFailure = Result.Ok();
        foreach (TInput item in source)
        {
            if (token.IsCancellationRequested)
                break;

            ValueTask<Result> stepTask = step(item, token);
            Result stepResult = stepTask.IsCompletedSuccessfully ? stepTask.Result : await stepTask;
            if (stepResult.IsSuccess)
                continue;

            abortedOn = item;
            iterationFailure = stepResult;
            break;
        }

        return EachCoreVoidFinalize(abortedOn, iterationFailure, token);
    }

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронное действие для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result> EachCoreVoidSync<TInput>(
        IEnumerable<TInput> source,
        Action<TInput> step,
        CancellationToken token) =>
        ValueTask.FromResult(EachCoreVoidSyncCore(source, step, token));

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result> EachCoreVoidSync<TInput>(
        IEnumerable<TInput> source,
        Func<TInput, Result> step,
        CancellationToken token) =>
        ValueTask.FromResult(EachCoreVoidSyncCore(source, step, token));

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронное действие для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result> EachCoreVoidSync<TInput>(
        IEnumerable<TInput> source,
        Action<TInput, CancellationToken> step,
        CancellationToken token) =>
        ValueTask.FromResult(EachCoreVoidSyncCore(source, step, token));

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static ValueTask<Result> EachCoreVoidSync<TInput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, Result> step,
        CancellationToken token) =>
        ValueTask.FromResult(EachCoreVoidSyncCore(source, step, token));

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронное действие для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static Result EachCoreVoidSyncCore<TInput>(
        IEnumerable<TInput> source,
        Action<TInput> step,
        CancellationToken token)
    {
        foreach (TInput item in source)
        {
            if (token.IsCancellationRequested)
                break;
            step(item);
        }
        return EachCoreVoidFinalize<TInput>(default, Result.Ok(), token);
    }

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static Result EachCoreVoidSyncCore<TInput>(
        IEnumerable<TInput> source,
        Func<TInput, Result> step,
        CancellationToken token)
    {
        TInput abortedOn = default;
        Result iterationFailure = Result.Ok();
        foreach (TInput item in source)
        {
            if (token.IsCancellationRequested)
                break;

            Result stepResult = step(item);
            if (stepResult.IsSuccess)
                continue;

            abortedOn = item;
            iterationFailure = stepResult;
            break;
        }
        return EachCoreVoidFinalize(abortedOn, iterationFailure, token);
    }

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронное действие для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static Result EachCoreVoidSyncCore<TInput>(
        IEnumerable<TInput> source,
        Action<TInput, CancellationToken> step,
        CancellationToken token)
    {
        foreach (TInput item in source)
        {
            if (token.IsCancellationRequested)
                break;
            step(item, token);
        }
        return EachCoreVoidFinalize<TInput>(default, Result.Ok(), token);
    }

    /// <summary>
    /// Синхронно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static Result EachCoreVoidSyncCore<TInput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, Result> step,
        CancellationToken token)
    {
        TInput abortedOn = default;
        Result iterationFailure = Result.Ok();
        foreach (TInput item in source)
        {
            if (token.IsCancellationRequested)
                break;

            Result stepResult = step(item, token);
            if (stepResult.IsSuccess)
                continue;

            abortedOn = item;
            iterationFailure = stepResult;
            break;
        }
        return EachCoreVoidFinalize(abortedOn, iterationFailure, token);
    }
    #endregion

}
