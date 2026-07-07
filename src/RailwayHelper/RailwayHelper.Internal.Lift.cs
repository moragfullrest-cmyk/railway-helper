using System.Collections;
using System.Linq.Expressions;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Lift

    /// <summary>
    /// Адаптеры делегатов публичного API к унифицированным сигнатурам шагов pipeline.
    /// </summary>
    private static class Lift
    {
        /// <summary>Преобразует синхронный делегат без входа в <see cref="Func{CancellationToken, ValueTask{Result}}"/>.</summary>
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<TOutput> f)
        {
            var handler = EnsureHandler(f);
            return token => ValueTask.FromResult(Result.Ok<TOutput>(handler()));
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<Task<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return _ => LiftOk(handler());
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<Result<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return token => ValueTask.FromResult(handler());
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<Task<Result<TOutput>>> f)
        {
            var handler = EnsureHandler(f);
            return ct => new ValueTask<Result<TOutput>>(handler());
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<CancellationToken, TOutput> f)
        {
            var handler = EnsureHandler(f);
            return token => ValueTask.FromResult(Result.Ok<TOutput>(handler(token)));
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<CancellationToken, Result<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return token => ValueTask.FromResult(handler(token));
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<CancellationToken, Task<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return token => LiftOk(handler(token));
        }
        public static Func<CancellationToken, ValueTask<Result<TOutput>>> FromNoInput<TOutput>(Func<CancellationToken, Task<Result<TOutput>>> f)
        {
            var handler = EnsureHandler(f);
            return ct => new ValueTask<Result<TOutput>>(handler(ct));
        }

        /// <summary>Преобразует синхронный void-делегат без входа в <see cref="Func{CancellationToken, ValueTask{Result}}"/>.</summary>
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Action f)
        {
            var handler = EnsureHandler(f);
            return token => { handler(); return ValueTask.FromResult(Result.Ok()); };
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Func<Task> f)
        {
            var handler = EnsureHandler(f);
            return _ => LiftVoid(handler());
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Func<Result> f)
        {
            var handler = EnsureHandler(f);
            return token => ValueTask.FromResult(handler());
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Func<Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return ct => new ValueTask<Result>(handler());
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Action<CancellationToken> f)
        {
            var handler = EnsureHandler(f);
            return token => { handler(token); return ValueTask.FromResult(Result.Ok()); };
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Func<CancellationToken, Task> f)
        {
            var handler = EnsureHandler(f);
            return token => LiftVoid(handler(token));
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Func<CancellationToken, Result> f)
        {
            var handler = EnsureHandler(f);
            return token => ValueTask.FromResult(handler(token));
        }
        public static Func<CancellationToken, ValueTask<Result>> FromNoInputVoid(Func<CancellationToken, Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return ct => new ValueTask<Result>(handler(ct));
        }

        /// <summary>Преобразует void-делегат с входом в <see cref="Func{TInput, CancellationToken, ValueTask{Result}}"/>.</summary>
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Action<TInput> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) =>
            {
                handler(x);
                return ValueTask.FromResult(Result.Ok());
            };
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Action f)
        {
            var handler = EnsureHandler(f);
            return (x, _) =>
            {
                handler();
                return ValueTask.FromResult(Result.Ok());
            };
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<Task> f)
        {
            var handler = EnsureHandler(f);
            return (_, _) => LiftVoid(handler());
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<TInput, Task> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => LiftVoid(handler(x));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<TInput, Result> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => ValueTask.FromResult(handler(x));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<TInput, Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => new ValueTask<Result>(handler(x));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Action<TInput, CancellationToken> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) =>
            {
                handler(x, ct);
                return ValueTask.FromResult(Result.Ok());
            };
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<TInput, CancellationToken, Task> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => LiftVoid(handler(x, ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<TInput, CancellationToken, Result> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => ValueTask.FromResult(handler(x, ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<TInput, CancellationToken, Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => new ValueTask<Result>(handler(x, ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<CancellationToken, Task> f)
        {
            var handler = EnsureHandler(f);
            return (_, ct) => LiftVoid(handler(ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<CancellationToken, Result> f)
        {
            var handler = EnsureHandler(f);
            return (_, ct) => ValueTask.FromResult(handler(ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Func<CancellationToken, Task<Result>> f)
        {
            var handler = EnsureHandler(f);
            return (_, ct) => new ValueTask<Result>(handler(ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result>> FromVoid<TInput>(Action<CancellationToken> f)
        {
            var handler = EnsureHandler(f);
            return (_, ct) =>
            {
                handler(ct);
                return ValueTask.FromResult(Result.Ok());
            };
        }

        /// <summary>Преобразует делегат с входом в <see cref="Func{TInput, CancellationToken, ValueTask{Result}}"/>.</summary>
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, TOutput> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => ValueTask.FromResult(Result.Ok<TOutput>(handler(x)));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, Result<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => ValueTask.FromResult(handler(x));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, Task<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => LiftOk(handler(x));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, Task<Result<TOutput>>> f)
        {
            var handler = EnsureHandler(f);
            return (x, _) => new ValueTask<Result<TOutput>>(handler(x));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, CancellationToken, TOutput> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => ValueTask.FromResult(Result.Ok<TOutput>(handler(x, ct)));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, CancellationToken, Result<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => ValueTask.FromResult(handler(x, ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, CancellationToken, Task<TOutput>> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => LiftOk(handler(x, ct));
        }
        public static Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> From<TInput, TOutput>(Func<TInput, CancellationToken, Task<Result<TOutput>>> f)
        {
            var handler = EnsureHandler(f);
            return (x, ct) => new ValueTask<Result<TOutput>>(handler(x, ct));
        }

        private static TDelegate EnsureHandler<TDelegate>(TDelegate handler)
            where TDelegate : class =>
            handler ?? throw new ArgumentNullException(nameof(handler));
    }
    #endregion

}
