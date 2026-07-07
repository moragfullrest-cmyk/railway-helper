using System.Collections;
using System.Linq.Expressions;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Peek

    /// <summary>
    /// Внутренняя реализация <see cref="Peek"/>: побочный эффект над значением предыдущего шага без его изменения.
    /// </summary>
    /// <typeparam name="TInput">Тип значения предыдущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult<TInput>> PeekInternal<TInput>(
        ValueTask<RopResult<TInput>> inputTask,
        Func<TInput, CancellationToken, ValueTask<Result>> func,
        string label = null) =>
        ExecutePipelineStep(inputTask, (previous, token) =>
            (previous.Token.IsCancellationRequested, previous.Result) switch
            {
                (true, _) => ValueTask.FromResult(FailCancelled<TInput>(token)),
                (_, { IsSuccess: false, Errors: var errors }) => ValueTask.FromResult(FailWithErrors<TInput>(errors, token)),
                (_, { IsSuccess: true, Value: null }) => ValueTask.FromResult(FailNoData<TInput>(token)),
                (_, { IsSuccess: true, Value: var value }) => PeekInternalSuccess(func, value, label, token),
            });

    /// <summary>
    /// Внутренняя реализация <see cref="PeekEach"/>: побочный эффект для каждого элемента коллекции без её изменения.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <param name="input">Задача с коллекцией из предыдущего шага.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult<IEnumerable<TInput>>> PeekEachInternal<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        string label) =>
        NextInternal<IEnumerable<TInput>, IEnumerable<TInput>>(
            input,
            (items, token) => PeekEachInternalStep(items, step, token),
            label);

    private static async ValueTask<Result<IEnumerable<TInput>>> PeekEachInternalStep<TInput>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        CancellationToken token)
    {
        Result voidResult = await EachCoreVoid(items, step, token);
        return voidResult.IsSuccess ? Result.Ok(items) : voidResult;
    }

    private static ValueTask<RopResult<IEnumerable<TInput>>> PeekEachInternalSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Action<TInput> step,
        string label) =>
        NextInternal<IEnumerable<TInput>, IEnumerable<TInput>>(
            input,
            (items, token) => PeekEachInternalStepSync(items, step, token),
            label);

    private static ValueTask<RopResult<IEnumerable<TInput>>> PeekEachInternalSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Func<TInput, Result> step,
        string label) =>
        NextInternal<IEnumerable<TInput>, IEnumerable<TInput>>(
            input,
            (items, token) => PeekEachInternalStepSync(items, step, token),
            label);

    private static ValueTask<RopResult<IEnumerable<TInput>>> PeekEachInternalSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Action<TInput, CancellationToken> step,
        string label) =>
        NextInternal<IEnumerable<TInput>, IEnumerable<TInput>>(
            input,
            (items, token) => PeekEachInternalStepSync(items, step, token),
            label);

    private static ValueTask<RopResult<IEnumerable<TInput>>> PeekEachInternalSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Func<TInput, CancellationToken, Result> step,
        string label) =>
        NextInternal<IEnumerable<TInput>, IEnumerable<TInput>>(
            input,
            (items, token) => PeekEachInternalStepSync(items, step, token),
            label);

    private static ValueTask<Result<IEnumerable<TInput>>> PeekEachInternalStepSync<TInput>(
        IEnumerable<TInput> items,
        Action<TInput> step,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(items, step, token);
        return ValueTask.FromResult(voidResult.IsSuccess ? Result.Ok<IEnumerable<TInput>>(items) : voidResult);
    }

    private static ValueTask<Result<IEnumerable<TInput>>> PeekEachInternalStepSync<TInput>(
        IEnumerable<TInput> items,
        Func<TInput, Result> step,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(items, step, token);
        return ValueTask.FromResult(voidResult.IsSuccess ? Result.Ok<IEnumerable<TInput>>(items) : voidResult);
    }

    private static ValueTask<Result<IEnumerable<TInput>>> PeekEachInternalStepSync<TInput>(
        IEnumerable<TInput> items,
        Action<TInput, CancellationToken> step,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(items, step, token);
        return ValueTask.FromResult(voidResult.IsSuccess ? Result.Ok<IEnumerable<TInput>>(items) : voidResult);
    }

    private static ValueTask<Result<IEnumerable<TInput>>> PeekEachInternalStepSync<TInput>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Result> step,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(items, step, token);
        return ValueTask.FromResult(voidResult.IsSuccess ? Result.Ok<IEnumerable<TInput>>(items) : voidResult);
    }
    private static async ValueTask<RopResult<TInput>> PeekInternalSuccess<TInput>(
        Func<TInput, CancellationToken, ValueTask<Result>> func,
        TInput value,
        string label,
        CancellationToken token) =>
        await func(value, token) switch
        {
            { IsSuccess: false, Errors: var errors } =>
                new RopResult<TInput>(AttachContext<TInput>(Result.Fail<TInput>(errors), label, value), token),
            _ => new RopResult<TInput>(Result.Ok(value), token),
        };
    #endregion

}
