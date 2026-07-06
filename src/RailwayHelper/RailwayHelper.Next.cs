namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Next

    /// <summary>
    /// Следующий шаг pipeline с побочным эффектом над значением предыдущего шага.
    /// При ошибке на предыдущем шаге текущий не выполняется.
    /// </summary>
    /// <typeparam name="TInput">Тип значения предыдущего шага.</typeparam>
    /// <param name="input">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронное действие над значением.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <returns>Результат шага pipeline.</returns>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<TInput, Task> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<TInput, Result> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<TInput, Task<Result>> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для синхронного <see cref="Action{T}"/>.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Action<TInput> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="Action"/> без использования значения предыдущего шага.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Action func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Следующий шаг после шага без значения; побочный эффект.</summary>
    public static async Task<RopResult> Next(this Task<RopResult> input, Action func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task"/>.</summary>
    public static async Task<RopResult> Next(this Task<RopResult> input, Func<Task> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Next(this Task<RopResult> input, Func<Result> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult> Next(this Task<RopResult> input, Func<Task<Result>> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);

    /// <summary>
    /// Следующий шаг после шага без значения; возвращает новое значение в pipeline.
    /// </summary>
    /// <typeparam name="TOutput">Тип результата шага.</typeparam>
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<TOutput> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<Result<TOutput>> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{TResult}"/>.</summary>
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<Task<TOutput>> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<Task<Result<TOutput>>> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);

    /// <summary>
    /// Следующий шаг pipeline: преобразует значение предыдущего шага.
    /// </summary>
    /// <typeparam name="TInput">Тип значения предыдущего шага.</typeparam>
    /// <typeparam name="TOutput">Тип результата текущего шага.</typeparam>
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, TOutput> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result{TValue}"/>.</summary>
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, Result<TOutput>> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{TResult}"/>.</summary>
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, Task<TOutput>> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, Task<Result<TOutput>>> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);
    #endregion

    #region Next + Cancellation

    /// <summary>
    /// Следующий шаг pipeline с передачей <see cref="CancellationToken"/> в делегат.
    /// </summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="Action{T1, T2}"/> с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Action<TInput, CancellationToken> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата только с <see cref="CancellationToken"/> (значение предыдущего шага не используется).</summary>
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<CancellationToken, Task> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<CancellationToken, Result> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Func<CancellationToken, Task<Result>> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static async Task<RopResult> Next<TInput>(this Task<RopResult<TInput>> input, Action<CancellationToken> func, string label = null) =>
         await NextSideEffectInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static async Task<RopResult> Next(this Task<RopResult> input, Action<CancellationToken> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);
    public static async Task<RopResult> Next(this Task<RopResult> input, Func<CancellationToken, Task> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);
    public static async Task<RopResult> Next(this Task<RopResult> input, Func<CancellationToken, Result> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);
    public static async Task<RopResult> Next(this Task<RopResult> input, Func<CancellationToken, Task<Result>> func, string label = null) =>
         await NextSideEffectInternal(input, Lift.FromNoInputVoid(func), label);
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<CancellationToken, TOutput> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<CancellationToken, Result<TOutput>> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<CancellationToken, Task<TOutput>> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);
    public static async Task<RopResult<TOutput>> Next<TOutput>(this Task<RopResult> input, Func<CancellationToken, Task<Result<TOutput>>> func, string label = null) =>
        await NextValueInternal<TOutput>(input, Lift.FromNoInput<TOutput>(func), label);

    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, TOutput> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, Result<TOutput>> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, Task<TOutput>> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);
    public static async Task<RopResult<TOutput>> Next<TInput, TOutput>(this Task<RopResult<TInput>> input, Func<TInput, CancellationToken, Task<Result<TOutput>>> func, string label = null) =>
        await NextInternal<TInput, TOutput>(input, Lift.From<TInput, TOutput>(func), label);

    #endregion

    #region Next + each

    /// <summary>
    /// Следующий шаг pipeline: последовательно применяет побочный эффект к каждому элементу коллекции
    /// из результата предыдущего шага.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <param name="input">Задача с коллекцией из предыдущего шага.</param>
    /// <param name="func">Асинхронное действие для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, Task> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для синхронного <see cref="Action{T}"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Action<TInput> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, Result> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, Task<Result>> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>
    /// Следующий шаг pipeline: последовательно преобразует каждый элемент коллекции.
    /// </summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, Result<TOutput>> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);

    /// <summary>Перегрузка для синхронного преобразования элемента.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, TOutput> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, Task<Result<TOutput>>> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{TResult}"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, Task<TOutput>> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);

    /// <summary>Перегрузка для <see cref="IReadOnlyCollection{T}"/>; делегирует обработку через <see cref="IEnumerable{T}"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Task> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Action<TInput> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Result> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Task<Result>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Result<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, TOutput> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Task<Result<TOutput>>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Task<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);

    /// <summary>Перегрузка для <see cref="ICollection{T}"/>; делегирует обработку через <see cref="IEnumerable{T}"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, Task> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Action<TInput> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, Result> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, Task<Result>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, Result<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, TOutput> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, Task<Result<TOutput>>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, Task<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    #endregion

    #region Next + each + cancellation

    /// <summary>
    /// Следующий шаг pipeline: последовательная обработка коллекции с передачей <see cref="CancellationToken"/> в делегат.
    /// </summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="Action{T1, T2}"/> с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Action<TInput, CancellationToken> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
        await NextEachSideEffect<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для преобразования элементов с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Result<TOutput>> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, TOutput> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Task<Result<TOutput>>> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Task<TOutput>> func, string label = null) =>
        await NextInternal<IEnumerable<TInput>, IEnumerable<TOutput>>(input, (v, token) => EachCore<TInput, TOutput>(v, Lift.From<TInput, TOutput>(func), token), label);

    /// <summary>Перегрузка для <see cref="IReadOnlyCollection{T}"/> с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Action<TInput, CancellationToken> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Result<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, TOutput> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Task<Result<TOutput>>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Task<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);

    /// <summary>Перегрузка для <see cref="ICollection{T}"/> с <see cref="CancellationToken"/>.</summary>
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Action<TInput, CancellationToken> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult> NextEach<TInput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Result<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, TOutput> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Task<Result<TOutput>>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    public static async Task<RopResult<IEnumerable<TOutput>>> NextEach<TInput, TOutput>(this Task<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Task<TOutput>> func, string label = null) =>
        await AsEnumerable<TInput>(input).NextEach(func, label);
    #endregion
}
