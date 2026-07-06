namespace RailwayHelper;

/// <summary>
/// Построитель railway-oriented pipeline поверх <see cref="Result"/>.
/// Цепочки начинаются с <see cref="Do"/>, продолжаются через <see cref="Next"/>, <see cref="Peek"/>,
/// <see cref="NextEach"/> и <see cref="PeekEach"/>, завершаются обработчиком <see cref="OnFailure"/>.
/// </summary>
/// <remarks>
/// <para>Каждый шаг возвращает <see cref="RopResult"/> или <see cref="RopResult{TData}"/> —
/// обёртку над <see cref="Result"/> и актуальным <see cref="CancellationToken"/>.</para>
/// <para>При сбое шага с входными данными или меткой (<c>label</c>) к ошибкам добавляется
/// <see cref="ParametrizedError"/> — контекст входа (<see cref="ParametrizedError.Args"/>) и опциональная
/// метка для маршрутизации в <see cref="OnFailure"/>.</para>
/// <para><c>null</c> и пустые коллекции (кроме <see cref="string"/>) на шагах <see cref="Do"/> и <see cref="DoEach"/>
/// приводят к <see cref="NoDataError"/>; пустые коллекции на <see cref="Next"/> и <see cref="NextEach"/>
/// обрабатываются штатно, а осознанная реакция на отсутствие данных — через <see cref="IfNoData"/>.</para>
/// <para>Отмена операции — <see cref="CancelledError"/>; необработанное исключение — <see cref="ExceptionalError"/>.</para>
/// <para>Все ошибки конкретного pipeline обрабатываются в <see cref="OnFailure"/> и дальше не пробрасываются;
/// вызывающий код получает <see cref="HandledFailureError"/>.</para>
/// </remarks>
public static partial class RailwayHelper
{
    #region Errors

    /// <summary>Ошибка отмены операции по <see cref="CancellationToken"/>.</summary>
    public sealed class CancelledError : Error
    {
        public CancelledError() => Message = "Canceled";
    }

    /// <summary>Ошибка отсутствия или пустоты входных данных.</summary>
    public sealed class NoDataError : Error
    {
        public NoDataError() => Message = "NoData";
    }

    /// <summary>
    /// Ошибка прерывания последовательной обработки коллекции на конкретном элементе.
    /// </summary>
    /// <typeparam name="TAbortedOn">Тип элемента коллекции, на котором обработка была прервана.</typeparam>
    public sealed class SequenceAbortedError<TAbortedOn> : Error
    {
        /// <summary>Элемент коллекции, на котором обработка была прервана.</summary>
        public TAbortedOn AbortedOn { get; private set; }

        /// <summary>Создаёт ошибку с указанием элемента и причин прерывания.</summary>
        /// <param name="abortedOn">Элемент коллекции, на котором обработка остановлена.</param>
        /// <param name="causes">Причины сбоя, полученные при обработке элемента.</param>
        public SequenceAbortedError(TAbortedOn abortedOn, IEnumerable<IError> causes = null) : base("SequenceAborted")
        {
            AbortedOn = abortedOn;
            if (causes != null)
                CausedBy(causes);
        }
    }

    /// <summary>
    /// Pipeline намеренно прерван на пустых данных (после <see cref="IfNoData"/>).
    /// </summary>
    public sealed class PipelineTerminatedError : Error
    {
        public PipelineTerminatedError() => Message = "PipelineTerminated";
    }

    /// <summary>
    /// Контекст обработки пустой коллекции в <see cref="IfNoData"/>.
    /// </summary>
    public sealed class NoDataContext(CancellationToken cancellationToken)
    {
        /// <summary>Токен отмены шага pipeline.</summary>
        public CancellationToken Token { get; } = cancellationToken;

        internal bool IsTerminated { get; private set; }

        /// <summary>Прерывает pipeline с <see cref="PipelineTerminatedError"/>.</summary>
        public void Terminate() => IsTerminated = true;
    }

    /// <summary>
    /// Маркер ошибки, обработанной в <see cref="OnFailure"/>.
    /// Исходные ошибки шагов pipeline в итоговый <see cref="Result"/> не попадают.
    /// </summary>
    public sealed class HandledFailureError : Error { }

    /// <summary>
    /// Контекстная ошибка шага pipeline: входные данные, на которых произошёл сбой.
    /// <see cref="Label"/> опционален и служит для маршрутизации через <see cref="WhenCallData"/>.
    /// </summary>
    /// <param name="label">Метка шага (<c>label</c> из <see cref="Do"/> / <see cref="Next"/>), если задана.</param>
    /// <param name="args">Входные данные шага в момент сбоя.</param>
    public sealed class ParametrizedError(string label, object args) : Error
    {
        /// <summary>Метка шага, на котором произошла ошибка, если была указана.</summary>
        public string Label { get; } = label;

        /// <summary>Входные данные шага в момент сбоя.</summary>
        public object Args { get; } = args;

        /// <summary><c>true</c>, если задан контекст входных данных.</summary>
        public bool HasArgs => Args is not null;

        /// <summary><c>true</c>, если задана метка шага.</summary>
        public bool HasLabel => Label is not null;
    }

    #endregion

    #region Result

    /// <summary>Результат шага pipeline с типизированным значением.</summary>
    /// <typeparam name="TData">Тип значения результата.</typeparam>
    /// <param name="Result">Результат FluentResults.</param>
    /// <param name="Token">Актуальный токен отмены шага.</param>
    public sealed record RopResult<TData>(Result<TData> Result, CancellationToken Token)
    {
        public static implicit operator Result<TData>(RopResult<TData> result) => result.Result;
    }

    /// <summary>Результат шага pipeline без возвращаемого значения.</summary>
    /// <param name="Result">Результат FluentResults.</param>
    /// <param name="Token">Актуальный токен отмены шага.</param>
    public sealed record RopResult(Result Result, CancellationToken Token)
    {
        public static implicit operator Result(RopResult result) => result.Result;
    }

    /// <summary>Преобразует типизированный <see cref="RopResult{TData}"/> в нетипизированный <see cref="RopResult"/>.</summary>
    public static RopResult ToUntyped<TType>(this RopResult<TType> result) => new(result.Result.ToResult(), result.Token);

    /// <summary>Преобразует нетипизированный <see cref="RopResult"/> в типизированный <see cref="RopResult{TData}"/>.</summary>
    public static RopResult<TType> FromUntyped<TType>(this RopResult result) => new(result.Result.ToResult<TType>(), result.Token);

    /// <summary>
    /// Извлекает последнюю <see cref="ParametrizedError"/> с непустым <see cref="ParametrizedError.Args"/>.
    /// </summary>
    /// <param name="result">Результат с ошибками.</param>
    /// <param name="error">Найденная параметризованная ошибка.</param>
    /// <returns><c>true</c>, если <see cref="ParametrizedError"/> с контекстом входа найдена.</returns>
    public static bool TryGetCallData(this ResultBase result, out ParametrizedError error)
    {
        error = result.Errors?.LastOrDefault(e => e is ParametrizedError { HasArgs: true }) as ParametrizedError;
        return error is not null;
    }

    /// <summary>
    /// Извлекает последнюю <see cref="ParametrizedError"/> с указанной меткой шага.
    /// </summary>
    /// <param name="result">Результат с ошибками.</param>
    /// <param name="label">Ожидаемая метка шага.</param>
    /// <param name="error">Найденная параметризованная ошибка.</param>
    /// <returns><c>true</c>, если <see cref="ParametrizedError"/> найдена и её <see cref="ParametrizedError.Label"/> совпадает с <paramref name="label"/>.</returns>
    public static bool TryGetCallData(this ResultBase result, string label, out ParametrizedError error)
    {
        error = result.Errors?.LastOrDefault(e => e is ParametrizedError pe && pe.Label == label) as ParametrizedError;
        return error is not null;
    }

    /// <summary>
    /// Извлекает контекст входных данных шага с указанной меткой.
    /// </summary>
    /// <typeparam name="TArgs">Тип контекста <see cref="ParametrizedError.Args"/>.</typeparam>
    /// <param name="result">Результат с ошибками.</param>
    /// <param name="label">Ожидаемая метка шага.</param>
    /// <param name="args">Контекст входных данных шага.</param>
    /// <returns><c>true</c>, если контекст найден и приведён к <typeparamref name="TArgs"/>.</returns>
    public static bool TryGetCallData<TArgs>(this ResultBase result, string label, out TArgs args)
    {
        if (result.TryGetCallData(label, out ParametrizedError error) && error.Args is TArgs typed)
        {
            args = typed;
            return true;
        }

        args = default;
        return false;
    }

    /// <summary>
    /// Вызывает обработчик, если в списке ошибок найдена <see cref="ParametrizedError"/> с <paramref name="label"/>.
    /// Предназначен для использования внутри <see cref="OnFailure"/> при нескольких метках шага.
    /// </summary>
    /// <param name="result">Результат с ошибками.</param>
    /// <param name="label">Ожидаемая метка шага.</param>
    /// <param name="action">Обработчик ошибки.</param>
    public static void WhenCallData(this ResultBase result, string label, Action<ResultBase> action)
    {
        if (result.TryGetCallData(label, out _))
            action(result);
    }

    /// <summary>Перегрузка с контекстом <see cref="ParametrizedError"/>.</summary>
    public static void WhenCallData(this ResultBase result, string label, Action<ParametrizedError, ResultBase> action)
    {
        if (result.TryGetCallData(label, out ParametrizedError error))
            action(error, result);
    }

    /// <summary>Перегрузка с типизированным контекстом входных данных шага.</summary>
    public static void WhenCallData<TArgs>(this ResultBase result, string label, Action<TArgs, ResultBase> action)
    {
        if (result.TryGetCallData<TArgs>(label, out TArgs args))
            action(args, result);
    }

    /// <summary>
    /// Возвращает <c>true</c>, если pipeline прерван через <see cref="NoDataContext.Terminate"/>.
    /// </summary>
    public static bool IsPipelineTerminated(this ResultBase result) =>
        result.Errors.FirstOrDefault() is PipelineTerminatedError;
    #endregion

    #region EndPoints

    /// <summary>
    /// Завершает pipeline: при ошибке вызывает обработчик и возвращает <see cref="HandledFailureError"/>.
    /// </summary>
    /// <remarks>
    /// <para>Все ошибки конкретного pipeline должны быть обработаны в этом обработчике
    /// (логирование, уведомления и т.п.). Исходные ошибки шагов дальше не пробрасываются —
    /// итоговый <see cref="Result"/> содержит только <see cref="HandledFailureError"/>.</para>
    /// <para>Для маршрутизации по меткам шагов используйте <see cref="WhenCallData"/>.</para>
    /// </remarks>
    /// <typeparam name="TInput">Тип значения результата.</typeparam>
    /// <param name="inputTask">Задача с результатом pipeline.</param>
    /// <param name="func">Асинхронный обработчик ошибки.</param>
    /// <returns>Итоговый <see cref="Result{TValue}"/>.</returns>
    public static async Task<Result<TInput>> OnFailure<TInput>(this Task<RopResult<TInput>> inputTask, Func<ResultBase, Task> func) =>
        (await inputTask) switch
        {
            { Result.IsFailed: true, Result: var failed } => await OnHandledFailureAsync<TInput>(failed, func),
            { Result: var result } => result,
        };

    /// <inheritdoc cref="OnFailure{TInput}(Task{RopResult{TInput}}, Func{ResultBase, Task})"/>
    public static async Task<Result> OnFailure(this Task<RopResult> inputTask, Func<ResultBase, Task> func) =>
        (await inputTask) switch
        {
            { Result.IsFailed: true, Result: var failed } => await OnHandledFailureAsync(failed, func),
            { Result: var result } => result,
        };

    /// <inheritdoc cref="OnFailure{TInput}(Task{RopResult{TInput}}, Func{ResultBase, Task})"/>
    public static Task<Result<TInput>> OnFailure<TInput>(this Task<RopResult<TInput>> inputTask, Action<ResultBase> func) =>
        inputTask.OnFailure<TInput>(failed => { func(failed); return Task.CompletedTask; });

    /// <inheritdoc cref="OnFailure{TInput}(Task{RopResult{TInput}}, Func{ResultBase, Task})"/>
    public static Task<Result> OnFailure(this Task<RopResult> inputTask, Action<ResultBase> func) =>
        inputTask.OnFailure(failed => { func(failed); return Task.CompletedTask; });

    /// <inheritdoc cref="OnFailure{TInput}(Task{RopResult{TInput}}, Func{ResultBase, Task})"/>
    public static async Task<Result<TInput>> OnFailure<TInput>(this Task<RopResult<TInput>> inputTask, Func<ResultBase, CancellationToken, Task> func) =>
        (await inputTask) switch
        {
            { Result.IsFailed: true, Result: var failed, Token: var token } => await OnHandledFailureAsync<TInput>(failed, func, token),
            { Result: var result } => result,
        };

    /// <inheritdoc cref="OnFailure{TInput}(Task{RopResult{TInput}}, Func{ResultBase, Task})"/>
    public static async Task<Result> OnFailure(this Task<RopResult> inputTask, Func<ResultBase, CancellationToken, Task> func) =>
        (await inputTask) switch
        {
            { Result.IsFailed: true, Result: var failed, Token: var token } => await OnHandledFailureAsync(failed, func, token),
            { Result: var result } => result,
        };

    /// <summary>
    /// Вызывает обработчик ошибки и возвращает <see cref="HandledFailureError"/>.
    /// </summary>
    /// <param name="failed">Неуспешный результат pipeline.</param>
    /// <param name="func">Асинхронный обработчик ошибки.</param>
    private static async Task<Result> OnHandledFailureAsync(ResultBase failed, Func<ResultBase, Task> func)
    {
        await func(failed);
        return Result.Fail(new HandledFailureError());
    }

    /// <summary>
    /// Вызывает обработчик ошибки и возвращает типизированный результат с <see cref="HandledFailureError"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="failed">Неуспешный результат pipeline.</param>
    /// <param name="func">Асинхронный обработчик ошибки.</param>
    private static async Task<Result<T>> OnHandledFailureAsync<T>(ResultBase failed, Func<ResultBase, Task> func)
    {
        await func(failed);
        return Result.Fail<T>(new HandledFailureError());
    }

    /// <summary>
    /// Вызывает обработчик ошибки с <see cref="CancellationToken"/> и возвращает <see cref="HandledFailureError"/>.
    /// </summary>
    /// <param name="failed">Неуспешный результат pipeline.</param>
    /// <param name="func">Асинхронный обработчик ошибки с токеном отмены.</param>
    /// <param name="token">Токен отмены шага.</param>
    private static async Task<Result> OnHandledFailureAsync(ResultBase failed, Func<ResultBase, CancellationToken, Task> func, CancellationToken token)
    {
        await func(failed, token);
        return Result.Fail(new HandledFailureError());
    }

    /// <summary>
    /// Вызывает обработчик ошибки с <see cref="CancellationToken"/> и возвращает типизированный результат с <see cref="HandledFailureError"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="failed">Неуспешный результат pipeline.</param>
    /// <param name="func">Асинхронный обработчик ошибки с токеном отмены.</param>
    /// <param name="token">Токен отмены шага.</param>
    private static async Task<Result<T>> OnHandledFailureAsync<T>(ResultBase failed, Func<ResultBase, CancellationToken, Task> func, CancellationToken token)
    {
        await func(failed, token);
        return Result.Fail<T>(new HandledFailureError());
    }

    #endregion

}
