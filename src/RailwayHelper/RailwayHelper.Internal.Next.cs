using System.Collections;
using System.Linq.Expressions;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Next

    /// <summary>
    /// Внутренняя реализация <see cref="NextEach"/> с побочным эффектом для каждого элемента коллекции.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <param name="input">Задача с коллекцией из предыдущего шага.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult> NextEachSideEffect<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        string label) =>
        NextSideEffectInternal<IEnumerable<TInput>>(input, (v, token) => EachCoreVoid<TInput>(v, step, token), label);

    private static ValueTask<RopResult> NextEachSideEffectSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Action<TInput> step,
        string label) =>
        NextSideEffectInternal<IEnumerable<TInput>>(input, (v, token) => EachCoreVoidSync(v, step, token), label);

    private static ValueTask<RopResult> NextEachSideEffectSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Func<TInput, Result> step,
        string label) =>
        NextSideEffectInternal<IEnumerable<TInput>>(input, (v, token) => EachCoreVoidSync(v, step, token), label);

    private static ValueTask<RopResult> NextEachSideEffectSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Action<TInput, CancellationToken> step,
        string label) =>
        NextSideEffectInternal<IEnumerable<TInput>>(input, (v, token) => EachCoreVoidSync(v, step, token), label);

    private static ValueTask<RopResult> NextEachSideEffectSync<TInput>(
        ValueTask<RopResult<IEnumerable<TInput>>> input,
        Func<TInput, CancellationToken, Result> step,
        string label) =>
        NextSideEffectInternal<IEnumerable<TInput>>(input, (v, token) => EachCoreVoidSync(v, step, token), label);
    /// <summary>
    /// Внутренняя реализация <see cref="Next"/> с преобразованием значения предыдущего шага.
    /// </summary>
    /// <typeparam name="TInput">Тип значения предыдущего шага.</typeparam>
    /// <typeparam name="TOutput">Тип результата текущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронная функция преобразования.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult<TOutput>> NextInternal<TInput, TOutput>(
        ValueTask<RopResult<TInput>> inputTask,
        Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> func,
        string label = null) =>
        ExecutePipelineStep(inputTask, (previous, token) =>
            (previous.Token.IsCancellationRequested, previous.Result) switch
            {
                (true, _) => ValueTask.FromResult(FailCancelled<TOutput>(token)),
                (_, { IsSuccess: false, Errors: var errors }) => ValueTask.FromResult(FailWithErrors<TOutput>(errors, token)),
                (_, { IsSuccess: true, Value: null }) => ValueTask.FromResult(FailNoData<TOutput>(token)),
                (_, { IsSuccess: true, Value: var value }) => NextInternalSuccess(func, value, label, token),
            });

    private static async ValueTask<RopResult<TOutput>> NextInternalSuccess<TInput, TOutput>(
        Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> func,
        TInput value,
        string label,
        CancellationToken token) =>
        new RopResult<TOutput>(AttachContext<TOutput>(await func(value, token), label, value), token);
    /// <summary>
    /// Внутренняя реализация <see cref="Next"/> с побочным эффектом над значением предыдущего шага.
    /// </summary>
    /// <typeparam name="TInput">Тип значения предыдущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult> NextSideEffectInternal<TInput>(
        ValueTask<RopResult<TInput>> inputTask,
        Func<TInput, CancellationToken, ValueTask<Result>> func,
        string label = null) =>
        ExecutePipelineStep(inputTask, (previous, token) =>
            (previous.Token.IsCancellationRequested, previous.Result) switch
            {
                (true, _) => ValueTask.FromResult(FailCancelled(token)),
                (_, { IsSuccess: false, Errors: var errors }) => ValueTask.FromResult(FailWithErrors(errors, token)),
                (_, { IsSuccess: true, Value: null }) => ValueTask.FromResult(FailNoData(token)),
                (_, { IsSuccess: true, Value: var value }) => NextSideEffectInternalSuccess(func, value, label, token),
            });

    private static async ValueTask<RopResult> NextSideEffectInternalSuccess<TInput>(
        Func<TInput, CancellationToken, ValueTask<Result>> func,
        TInput value,
        string label,
        CancellationToken token) =>
        new RopResult(AttachContext(await func(value, token), label, value), token);

    /// <summary>
    /// Внутренняя реализация <see cref="Next"/> с побочным эффектом после шага без значения.
    /// </summary>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult> NextSideEffectInternal(
        ValueTask<RopResult> inputTask,
        Func<CancellationToken, ValueTask<Result>> func,
        string label = null) =>
        ExecutePipelineStep(inputTask, (previous, token) =>
            (previous.Token.IsCancellationRequested, previous.Result) switch
            {
                (true, _) => ValueTask.FromResult(FailCancelled(token)),
                (_, { IsSuccess: false, Errors: var errors }) => ValueTask.FromResult(FailWithErrors(errors, token)),
                (_, { IsSuccess: true }) => NextSideEffectInternalSuccess(func, label, token),
            });

    private static async ValueTask<RopResult> NextSideEffectInternalSuccess(
        Func<CancellationToken, ValueTask<Result>> func,
        string label,
        CancellationToken token) =>
        new RopResult(AttachContext(await func(token), label, null), token);

    /// <summary>
    /// Внутренняя реализация <see cref="Next"/> с возвратом нового значения после шага без значения.
    /// </summary>
    /// <typeparam name="TOutput">Тип результата шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    private static ValueTask<RopResult<TOutput>> NextValueInternal<TOutput>(
        ValueTask<RopResult> inputTask,
        Func<CancellationToken, ValueTask<Result<TOutput>>> func,
        string label = null) =>
        ExecutePipelineStep(inputTask, (previous, token) =>
            (previous.Token.IsCancellationRequested, previous.Result) switch
            {
                (true, _) => ValueTask.FromResult(FailCancelled<TOutput>(token)),
                (_, { IsSuccess: false, Errors: var errors }) => ValueTask.FromResult(FailWithErrors<TOutput>(errors, token)),
                (_, { IsSuccess: true }) => NextValueInternalSuccess(func, label, token),
            });

    private static async ValueTask<RopResult<TOutput>> NextValueInternalSuccess<TOutput>(
        Func<CancellationToken, ValueTask<Result<TOutput>>> func,
        string label,
        CancellationToken token) =>
        new RopResult<TOutput>(AttachContext<TOutput>(await func(token), label, null), token);
    #endregion

}
