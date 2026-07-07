using System.Collections;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region IfNoData

    /// <summary>
    /// Обрабатывает пустую коллекцию (не <c>null</c>).
    /// Обработчик возвращает коллекцию того же типа, что и вход pipeline, или вызывает
    /// <see cref="NoDataContext.Terminate"/>.
    /// </summary>
    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Action<NoDataContext> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, Task> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, Result> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, Task<Result>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Action<NoDataContext, CancellationToken> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, Task> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, Result> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, Task<Result>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.FromVoid(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, TCollection> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, Result<TCollection>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, Task<TCollection>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, Task<Result<TCollection>>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, TCollection> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, Result<TCollection>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, Task<TCollection>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, IfNoDataLift<TCollection>.From(onNoData));

    public static ValueTask<RopResult<TCollection>> IfNoData<TCollection>(
        this ValueTask<RopResult<TCollection>> input,
        Func<NoDataContext, CancellationToken, Task<Result<TCollection>>> onNoData)
        where TCollection : IEnumerable =>
        IfNoDataCore(input, (ctx, token) => new ValueTask<Result<TCollection>>(onNoData(ctx, token)));

    #endregion
}
