namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Do + no params + Cancellation

    /// <summary>
    /// Выполняет первый шаг pipeline без входных данных и возвращает значение.
    /// </summary>
    /// <typeparam name="TOutput">Тип результата шага.</typeparam>
    /// <param name="func">Синхронная функция шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат шага pipeline.</returns>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<TOutput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<Result<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{TResult}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<Task<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<Task<Result<TOutput>>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с параметром <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<CancellationToken, TOutput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<CancellationToken, Result<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<CancellationToken, Task<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TOutput>(Func<CancellationToken, Task<Result<TOutput>>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternalNoInput<TOutput>(Lift.FromNoInput<TOutput>(func), label, cancellationToken);

    #endregion

    #region Do + no params + side effect

    /// <summary>
    /// Выполняет первый шаг pipeline без входных данных; побочный эффект без возвращаемого значения.
    /// </summary>
    /// <param name="func">Синхронное действие шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат шага pipeline.</returns>
    public static async Task<RopResult> Do(Action func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task"/>.</summary>
    public static async Task<RopResult> Do(Func<Task> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Do(Func<Result> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult> Do(Func<Task<Result>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с параметром <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> Do(Action<CancellationToken> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> Do(Func<CancellationToken, Task> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Do(Func<CancellationToken, Result> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Do(Func<CancellationToken, Task<Result>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffectNoInput(Lift.FromNoInputVoid(func), label, cancellationToken);

    #endregion

    #region Do + params + Cancellation

    /// <summary>
    /// Выполняет первый шаг pipeline с входными данными и возвращает преобразованное значение.
    /// </summary>
    /// <typeparam name="TInput">Тип входных данных.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага.</typeparam>
    /// <param name="input">Входные данные шага.</param>
    /// <param name="func">Синхронная функция преобразования.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат шага pipeline.</returns>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, TOutput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, Result<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{TResult}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, Task<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, Task<Result<TOutput>>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, CancellationToken, TOutput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, CancellationToken, Result<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, CancellationToken, Task<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Do<TInput, TOutput>(TInput input, Func<TInput, CancellationToken, Task<Result<TOutput>>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label, cancellationToken);

    #endregion

    #region Do + params + side effect

    /// <summary>
    /// Выполняет первый шаг pipeline, передавая входные данные без преобразования (identity).
    /// </summary>
    /// <typeparam name="TInput">Тип входных данных.</typeparam>
    /// <param name="input">Входные данные шага.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Входные данные в обёртке pipeline.</returns>
    public static async Task<RopResult<TInput>> Do<TInput>(TInput input, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<TInput, TInput>(input, Lift.From<TInput, TInput>((_) => _), label, cancellationToken);

    /// <summary>
    /// Выполняет первый шаг pipeline с входными данными; побочный эффект без возвращаемого значения.
    /// </summary>
    /// <typeparam name="TInput">Тип входных данных.</typeparam>
    /// <param name="input">Входные данные шага.</param>
    /// <param name="func">Синхронное действие над входом.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат шага pipeline.</returns>
    public static async Task<RopResult> Do<TInput>(TInput input, Action<TInput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Func<TInput, Task> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Func<TInput, Result> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Func<TInput, Task<Result>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Action<TInput, CancellationToken> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Func<TInput, CancellationToken, Task> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Func<TInput, CancellationToken, Result> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Do<TInput>(TInput input, Func<TInput, CancellationToken, Task<Result>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    #endregion

    #region Do + each + cancellation

    /// <summary>
    /// Выполняет первый шаг pipeline: последовательно применяет функцию к каждому элементу коллекции.
    /// При сбое на элементе возвращает <see cref="SequenceAbortedError"/>.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата для элемента.</typeparam>
    /// <param name="input">Входная коллекция.</param>
    /// <param name="func">Синхронная функция для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Коллекция результатов при успехе всех элементов.</returns>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, TOutput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, Result<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{TResult}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, Task<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, Task<Result<TOutput>>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, TOutput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, Result<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, Task<TOutput>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> DoEach<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, Task<Result<TOutput>>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label, cancellationToken);

    #endregion

    #region Do + each + side effect

    /// <summary>
    /// Выполняет первый шаг pipeline: последовательно применяет побочный эффект к каждому элементу коллекции.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <param name="input">Входная коллекция.</param>
    /// <param name="func">Синхронное действие для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат шага pipeline.</returns>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Action<TInput> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Func<TInput, Task> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Func<TInput, Result> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Func<TInput, Task<Result>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Action<TInput, CancellationToken> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, Task> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, Result> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> DoEach<TInput>(IEnumerable<TInput> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null, CancellationToken cancellationToken = default) =>
        await DoEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label, cancellationToken);

    #endregion
}
