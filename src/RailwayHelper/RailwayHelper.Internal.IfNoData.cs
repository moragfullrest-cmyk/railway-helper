using System.Collections;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - IfNoData

    /// <summary>
    /// Внутренняя реализация <see cref="IfNoData"/> для пустых коллекций.
    /// </summary>
    /// <typeparam name="TCollection">Тип коллекции pipeline.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="onNoData">Обработчик пустой коллекции.</param>
    private static ValueTask<RopResult<TCollection>> IfNoDataCore<TCollection>(
        ValueTask<RopResult<TCollection>> inputTask,
        Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> onNoData)
        where TCollection : IEnumerable
    {
        ArgumentNullException.ThrowIfNull(onNoData);
        return ExecutePipelineStep(inputTask, (previous, token) =>
        {
            if (previous.Token.IsCancellationRequested)
                return ValueTask.FromResult(FailCancelled<TCollection>(token));

            return previous.Result switch
            {
                { IsSuccess: false, Errors: var errors } =>
                    ValueTask.FromResult(FailWithErrors<TCollection>(errors, token)),
                { IsSuccess: true, Value: null } =>
                    ValueTask.FromResult(FailNoData<TCollection>(token)),
                { IsSuccess: true, Value: var value } when !IsNullOrEmptyCollection(value) =>
                    ValueTask.FromResult(previous),
                { IsSuccess: true, Value: var value } =>
                    HandleEmptyCollectionAsync(value, onNoData, token),
            };
        });
    }

    private static async ValueTask<RopResult<TCollection>> HandleEmptyCollectionAsync<TCollection>(
        TCollection incoming,
        Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> onNoData,
        CancellationToken token)
        where TCollection : IEnumerable
    {
        NoDataContext context = new(token);
        Result<TCollection> handlerResult = await onNoData(context, token);

        return (token.IsCancellationRequested, context.IsTerminated, handlerResult) switch
        {
            (true, _, _) => FailCancelled<TCollection>(token),
            (_, true, _) => FailPipelineTerminated<TCollection>(token),
            (_, _, { IsSuccess: false, Errors: var errors }) =>
                FailWithErrors<TCollection>(errors, token),
            (_, _, { IsSuccess: true, Value: null }) =>
                new RopResult<TCollection>(Result.Ok(incoming), token),
            (_, _, var result) => new RopResult<TCollection>(result, token),
        };
    }

    /// <summary>
    /// Адаптеры делегатов <see cref="IfNoData"/> к унифицированной сигнатуре обработчика пустой коллекции.
    /// </summary>
    /// <typeparam name="TCollection">Тип коллекции pipeline.</typeparam>
    private static class IfNoDataLift<TCollection>
        where TCollection : IEnumerable
    {
        /// <summary>Преобразует синхронный делегат с <see cref="NoDataContext"/> в обработчик пустой коллекции.</summary>
        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, TCollection> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => ValueTask.FromResult(Result.Ok(handler(ctx)));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, Result<TCollection>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => ValueTask.FromResult(handler(ctx));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, Task<TCollection>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => LiftOk(handler(ctx));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, Task<Result<TCollection>>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => new ValueTask<Result<TCollection>>(handler(ctx));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, CancellationToken, TCollection> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) => ValueTask.FromResult(Result.Ok(handler(ctx, token)));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, CancellationToken, Result<TCollection>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) => ValueTask.FromResult(handler(ctx, token));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> From(Func<NoDataContext, CancellationToken, Task<TCollection>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) => LiftOk(handler(ctx, token));
        }

        /// <summary>Преобразует void-делегат с <see cref="NoDataContext"/> в обработчик пустой коллекции.</summary>
        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Action<NoDataContext> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) =>
            {
                handler(ctx);
                return ValueTask.FromResult(Result.Ok<TCollection>(default!));
            };
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Func<NoDataContext, Task> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => LiftVoidCollection(handler(ctx));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Func<NoDataContext, Result> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => ValueTask.FromResult(ToCollectionResult(handler(ctx)));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Func<NoDataContext, Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, _) => LiftCollectionResult(handler(ctx));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Action<NoDataContext, CancellationToken> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) =>
            {
                handler(ctx, token);
                return ValueTask.FromResult(Result.Ok<TCollection>(default!));
            };
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Func<NoDataContext, CancellationToken, Task> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) => LiftVoidCollection(handler(ctx, token));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Func<NoDataContext, CancellationToken, Result> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) => ValueTask.FromResult(ToCollectionResult(handler(ctx, token)));
        }

        public static Func<NoDataContext, CancellationToken, ValueTask<Result<TCollection>>> FromVoid(Func<NoDataContext, CancellationToken, Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return (ctx, token) => LiftCollectionResult(handler(ctx, token));
        }

        private static ValueTask<Result<TCollection>> LiftVoidCollection(Task task)
        {
            if (task.IsCompletedSuccessfully)
                return ValueTask.FromResult(Result.Ok<TCollection>(default!));
            return LiftVoidCollectionCore(task);
        }

        private static async ValueTask<Result<TCollection>> LiftVoidCollectionCore(Task task)
        {
            await task;
            return Result.Ok<TCollection>(default!);
        }

        private static ValueTask<Result<TCollection>> LiftCollectionResult(Task<Result> task)
        {
            if (task.IsCompletedSuccessfully)
                return ValueTask.FromResult(ToCollectionResult(task.Result));
            return LiftCollectionResultCore(task);
        }

        private static async ValueTask<Result<TCollection>> LiftCollectionResultCore(Task<Result> task) =>
            ToCollectionResult(await task);

        /// <summary>Преобразует <see cref="Result"/> void-обработчика в <see cref="Result{TCollection}"/>.</summary>
        /// <param name="result">Результат void-обработчика.</param>
        private static Result<TCollection> ToCollectionResult(Result result) =>
            result.IsSuccess ? Result.Ok<TCollection>(default!) : Result.Fail<TCollection>(result.Errors);

        private static TDelegate EnsureHandler<TDelegate>(TDelegate handler)
            where TDelegate : class =>
            handler ?? throw new ArgumentNullException(nameof(handler));
    }
    #endregion

}
