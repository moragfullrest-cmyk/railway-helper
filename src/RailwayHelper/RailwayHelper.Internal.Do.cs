using System.Collections;
using System.Linq.Expressions;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Do

    /// <summary>Маркер отсутствия входных данных для <see cref="DoInternal{TInput, TOutput}"/>.</summary>
    private readonly struct NoInput;

    /// <summary>
    /// Внутренняя реализация <see cref="Do"/> без входных данных с возвращаемым значением.
    /// </summary>
    /// <typeparam name="TOutput">Тип результата шага.</typeparam>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private static ValueTask<RopResult<TOutput>> DoInternalNoInput<TOutput>(
        Func<CancellationToken, ValueTask<Result<TOutput>>> func,
        string label,
        CancellationToken cancellationToken) =>
        DoInternal<NoInput, TOutput>(default, (_, token) => func(token), label, cancellationToken, hasInput: false);

    /// <summary>
    /// Внутренняя реализация <see cref="Do"/> без входных данных с побочным эффектом.
    /// </summary>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private static ValueTask<RopResult> DoSideEffectNoInput(
        Func<CancellationToken, ValueTask<Result>> func,
        string label,
        CancellationToken cancellationToken) =>
        DoSideEffect<NoInput>(default, (_, token) => func(token), label, cancellationToken, hasInput: false);

    /// <summary>
    /// Внутренняя реализация <see cref="Do"/> с входными данными и побочным эффектом.
    /// </summary>
    /// <typeparam name="TInput">Тип входных данных.</typeparam>
    /// <param name="input">Входные данные шага.</param>
    /// <param name="func">Асинхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private static ValueTask<RopResult> DoSideEffect<TInput>(
        TInput input,
        Func<TInput, CancellationToken, ValueTask<Result>> func,
        string label,
        CancellationToken cancellationToken,
        bool hasInput = true) =>
        Guard(token => (token.IsCancellationRequested, hasInput && IsNullOrEmptyCollection(input)) switch
        {
            (true, _) => ValueTask.FromResult(FailCancelled(token)),
            (_, true) => ValueTask.FromResult(FailNoData(token)),
            _ => DoSideEffectExecute(input, func, label, hasInput, token),
        }, cancellationToken);

    private static async ValueTask<RopResult> DoSideEffectExecute<TInput>(
        TInput input,
        Func<TInput, CancellationToken, ValueTask<Result>> func,
        string label,
        bool hasInput,
        CancellationToken token) =>
        new RopResult(AttachContext(await func(input, token), label, hasInput ? input : null), token);

    /// <summary>
    /// Внутренняя реализация <see cref="DoEach"/> с побочным эффектом для каждого элемента коллекции.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <param name="input">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private static ValueTask<RopResult> DoEachSideEffect<TInput>(
        IEnumerable<TInput> input,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        string label,
        CancellationToken cancellationToken) =>
        Guard(token => (token.IsCancellationRequested, IsNullOrEmptyCollection(input)) switch
        {
            (true, _) => ValueTask.FromResult(FailCancelled(token)),
            (_, true) => ValueTask.FromResult(FailNoData(token)),
            _ => DoEachSideEffectExecute(input, step, label, token),
        }, cancellationToken);

    private static async ValueTask<RopResult> DoEachSideEffectExecute<TInput>(
        IEnumerable<TInput> input,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        string label,
        CancellationToken token) =>
        new RopResult(AttachContext(await EachCoreVoid<TInput>(input, step, token), label, input), token);

    private static ValueTask<RopResult> DoEachSideEffectSync<TInput>(
        IEnumerable<TInput> input,
        Action<TInput> step,
        string label,
        CancellationToken cancellationToken) =>
        Guard(token => (token.IsCancellationRequested, IsNullOrEmptyCollection(input)) switch
        {
            (true, _) => ValueTask.FromResult(FailCancelled(token)),
            (_, true) => ValueTask.FromResult(FailNoData(token)),
            _ => DoEachSideEffectExecuteSync(input, step, label, token),
        }, cancellationToken);

    private static ValueTask<RopResult> DoEachSideEffectSync<TInput>(
        IEnumerable<TInput> input,
        Func<TInput, Result> step,
        string label,
        CancellationToken cancellationToken) =>
        Guard(token => (token.IsCancellationRequested, IsNullOrEmptyCollection(input)) switch
        {
            (true, _) => ValueTask.FromResult(FailCancelled(token)),
            (_, true) => ValueTask.FromResult(FailNoData(token)),
            _ => DoEachSideEffectExecuteSync(input, step, label, token),
        }, cancellationToken);

    private static ValueTask<RopResult> DoEachSideEffectSync<TInput>(
        IEnumerable<TInput> input,
        Action<TInput, CancellationToken> step,
        string label,
        CancellationToken cancellationToken) =>
        Guard(token => (token.IsCancellationRequested, IsNullOrEmptyCollection(input)) switch
        {
            (true, _) => ValueTask.FromResult(FailCancelled(token)),
            (_, true) => ValueTask.FromResult(FailNoData(token)),
            _ => DoEachSideEffectExecuteSync(input, step, label, token),
        }, cancellationToken);

    private static ValueTask<RopResult> DoEachSideEffectSync<TInput>(
        IEnumerable<TInput> input,
        Func<TInput, CancellationToken, Result> step,
        string label,
        CancellationToken cancellationToken) =>
        Guard(token => (token.IsCancellationRequested, IsNullOrEmptyCollection(input)) switch
        {
            (true, _) => ValueTask.FromResult(FailCancelled(token)),
            (_, true) => ValueTask.FromResult(FailNoData(token)),
            _ => DoEachSideEffectExecuteSync(input, step, label, token),
        }, cancellationToken);

    private static ValueTask<RopResult> DoEachSideEffectExecuteSync<TInput>(
        IEnumerable<TInput> input,
        Action<TInput> step,
        string label,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(input, step, token);
        return ValueTask.FromResult(new RopResult(AttachContext(voidResult, label, input), token));
    }

    private static ValueTask<RopResult> DoEachSideEffectExecuteSync<TInput>(
        IEnumerable<TInput> input,
        Func<TInput, Result> step,
        string label,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(input, step, token);
        return ValueTask.FromResult(new RopResult(AttachContext(voidResult, label, input), token));
    }

    private static ValueTask<RopResult> DoEachSideEffectExecuteSync<TInput>(
        IEnumerable<TInput> input,
        Action<TInput, CancellationToken> step,
        string label,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(input, step, token);
        return ValueTask.FromResult(new RopResult(AttachContext(voidResult, label, input), token));
    }

    private static ValueTask<RopResult> DoEachSideEffectExecuteSync<TInput>(
        IEnumerable<TInput> input,
        Func<TInput, CancellationToken, Result> step,
        string label,
        CancellationToken token)
    {
        Result voidResult = EachCoreVoidSyncCore(input, step, token);
        return ValueTask.FromResult(new RopResult(AttachContext(voidResult, label, input), token));
    }
    /// <summary>
    /// Внутренняя реализация <see cref="Do"/> с входными данными и возвращаемым значением.
    /// </summary>
    /// <typeparam name="TInput">Тип входных данных.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага.</typeparam>
    /// <param name="input">Входные данные шага.</param>
    /// <param name="func">Асинхронная функция преобразования.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private static ValueTask<RopResult<TOutput>> DoInternal<TInput, TOutput>(
       TInput input,
       Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> func,
       string label,
       CancellationToken cancellationToken,
       bool hasInput = true) =>
     Guard<TOutput>(token => (token.IsCancellationRequested, hasInput && IsNullOrEmptyCollection<TInput>(input)) switch
     {
         (true, _) => ValueTask.FromResult(FailCancelled<TOutput>(token)),
         (_, true) => ValueTask.FromResult(FailNoData<TOutput>(token)),
         _ => DoInternalExecute(input, func, label, hasInput, token),
     }, cancellationToken);

    private static async ValueTask<RopResult<TOutput>> DoInternalExecute<TInput, TOutput>(
        TInput input,
        Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> func,
        string label,
        bool hasInput,
        CancellationToken token) =>
        new RopResult<TOutput>(
            AttachContext<TOutput>(await func(input, token), label, hasInput ? input : null),
            token);
    #endregion

}
