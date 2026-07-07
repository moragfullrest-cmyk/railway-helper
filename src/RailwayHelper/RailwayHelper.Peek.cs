namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Peek

    /// <summary>
    /// Следующий шаг pipeline: побочный эффект над значением предыдущего шага без его изменения.
    /// При ошибке на предыдущем шаге текущий не выполняется.
    /// </summary>
    /// <typeparam name="TInput">Тип значения предыдущего шага.</typeparam>
    /// <param name="input">Задача с результатом предыдущего шага.</param>
    /// <param name="func">Асинхронное действие над значением.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    /// <returns>Результат шага pipeline с исходным значением.</returns>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<TInput, Task> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<TInput, Result> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<TInput, Task<Result>> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для синхронного <see cref="Action{T}"/>.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Action<TInput> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="Action"/> без использования значения предыдущего шага.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Action func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task"/> без использования значения предыдущего шага.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<Task> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    #endregion

    #region Peek + Cancellation

    /// <summary>
    /// Следующий шаг pipeline с передачей <see cref="CancellationToken"/> в делегат.
    /// </summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для асинхронного делегата с <see cref="CancellationToken"/>, возвращающего <see cref="Result"/>.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="Action{T1, T2}"/> с <see cref="CancellationToken"/>.</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Action<TInput, CancellationToken> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для делегата только с <see cref="CancellationToken"/> (значение предыдущего шага не используется).</summary>
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<CancellationToken, Task> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<CancellationToken, Result> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Func<CancellationToken, Task<Result>> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);
    public static ValueTask<RopResult<TInput>> Peek<TInput>(this ValueTask<RopResult<TInput>> input, Action<CancellationToken> func, string label = null) =>
         PeekInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    #endregion

    #region PeekEach

    /// <summary>
    /// Следующий шаг pipeline: последовательно применяет побочный эффект к каждому элементу коллекции
    /// из результата предыдущего шага без изменения коллекции.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента коллекции.</typeparam>
    /// <param name="input">Задача с коллекцией из предыдущего шага.</param>
    /// <param name="func">Асинхронное действие для элемента.</param>
    /// <param name="label">Метка шага для контекста ошибки.</param>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Func<TInput, Task> func, string label = null) =>
        PeekEachInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для синхронного <see cref="Action{T}"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Action<TInput> func, string label = null) =>
        PeekEachInternalSync<TInput>(input, func, label);

    /// <summary>Перегрузка для делегата, возвращающего <see cref="Result"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Func<TInput, Result> func, string label = null) =>
        PeekEachInternalSync<TInput>(input, func, label);

    /// <summary>Перегрузка для асинхронного делегата <see cref="Task{Result}"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Func<TInput, Task<Result>> func, string label = null) =>
        PeekEachInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="IReadOnlyCollection{T}"/>; делегирует обработку через <see cref="IEnumerable{T}"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Task> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Action<TInput> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Result> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, Task<Result>> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);

    /// <summary>Перегрузка для <see cref="ICollection{T}"/>; делегирует обработку через <see cref="IEnumerable{T}"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Func<TInput, Task> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Action<TInput> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Func<TInput, Result> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Func<TInput, Task<Result>> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    #endregion

    #region PeekEach + cancellation

    /// <summary>
    /// Следующий шаг pipeline: последовательная обработка коллекции с передачей <see cref="CancellationToken"/> в делегат.
    /// </summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
        PeekEachInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="Action{T1, T2}"/> с <see cref="CancellationToken"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Action<TInput, CancellationToken> func, string label = null) =>
        PeekEachInternalSync<TInput>(input, func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
        PeekEachInternalSync<TInput>(input, func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IEnumerable<TInput>>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
        PeekEachInternal<TInput>(input, Lift.FromVoid<TInput>(func), label);

    /// <summary>Перегрузка для <see cref="IReadOnlyCollection{T}"/> с <see cref="CancellationToken"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Action<TInput, CancellationToken> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<IReadOnlyCollection<TInput>>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);

    /// <summary>Перегрузка для <see cref="ICollection{T}"/> с <see cref="CancellationToken"/>.</summary>
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Task> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Action<TInput, CancellationToken> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Result> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    public static ValueTask<RopResult<IEnumerable<TInput>>> PeekEach<TInput>(this ValueTask<RopResult<ICollection<TInput>>> input, Func<TInput, CancellationToken, Task<Result>> func, string label = null) =>
        AsEnumerable<TInput>(input).PeekEach(func, label);
    #endregion
}
