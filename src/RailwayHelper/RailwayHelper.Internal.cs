using System.Collections;
using System.Linq.Expressions;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal

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

    /// <summary>
    /// Проверяет, что значение равно <c>null</c> или является пустой коллекцией (кроме <see cref="string"/>).
    /// </summary>
    /// <typeparam name="TInput">Тип проверяемого значения.</typeparam>
    /// <param name="value">Значение для проверки.</param>
    /// <returns><c>true</c>, если значение отсутствует или коллекция пуста.</returns>
    private static bool IsNullOrEmptyCollection<TInput>(TInput value) =>
        value is null
            || value is not string && EnumerableAny<TInput>.IsCollection && !EnumerableAny<TInput>.HasItems(value);

    /// <summary>
    /// Возвращает тип элемента <see cref="IEnumerable{T}"/> для коллекции, массива или <see cref="IGrouping{TKey, TElement}"/>.
    /// </summary>
    /// <param name="type">Тип коллекции.</param>
    private static Type GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return typeof(object);
    }

    /// <summary>
    /// Кэшированная проверка непустоты коллекций через скомпилированный вызов <see cref="Enumerable.Any{TSource}"/>.
    /// </summary>
    /// <typeparam name="T">Тип проверяемого значения.</typeparam>
    private static class EnumerableAny<T>
    {
        /// <summary>Скомпилированный делегат <see cref="Enumerable.Any{TSource}"/> для типа <typeparamref name="T"/>.</summary>
        private static readonly Func<T, bool> any;
#pragma warning disable S2743
        /// <summary><c>true</c>, если <typeparamref name="T"/> является коллекцией или массивом.</summary>
        public static readonly bool IsCollection;
#pragma warning restore S2743
        /// <summary>Инициализирует флаг <see cref="IsCollection"/> и компилирует делегат <c>any</c> при необходимости.</summary>
        static EnumerableAny()
        {
            Type type = typeof(T);
            IsCollection = type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

            if (!IsCollection)
                return;

            Type elementType = GetEnumerableElementType(type);
            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var anyMethod = typeof(Enumerable).GetMethods().First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 1)!.MakeGenericMethod(elementType);
            ParameterExpression param = Expression.Parameter(type, "x");
            Expression source = type == enumerableType ? param : Expression.Convert(param, enumerableType);
            any = Expression.Lambda<Func<T, bool>>(
              Expression.Call(null, anyMethod, source), param).Compile();
        }

        /// <summary>Проверяет, что коллекция содержит хотя бы один элемент.</summary>
        /// <param name="value">Значение коллекции.</param>
        /// <returns><c>true</c>, если коллекция не пуста.</returns>
        public static bool HasItems(T value) => any(value);
    }

    /// <summary>Преобразует <see cref="RopResult{IReadOnlyCollection}"/> в <see cref="RopResult{IEnumerable}"/>.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    private static ValueTask<RopResult<IEnumerable<T>>> AsEnumerable<T>(ValueTask<RopResult<IReadOnlyCollection<T>>> inputTask) =>
        AsEnumerableCore<T, IReadOnlyCollection<T>>(inputTask);

    /// <summary>Преобразует <see cref="RopResult{ICollection}"/> в <see cref="RopResult{IEnumerable}"/>.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    private static ValueTask<RopResult<IEnumerable<T>>> AsEnumerable<T>(ValueTask<RopResult<ICollection<T>>> inputTask) =>
        AsEnumerableCore<T, ICollection<T>>(inputTask);

    /// <summary>Преобразует <see cref="RopResult{TCollection}"/> в <see cref="RopResult{IEnumerable}"/>.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <typeparam name="TCollection">Тип исходной коллекции.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    private static async ValueTask<RopResult<IEnumerable<T>>> AsEnumerableCore<T, TCollection>(ValueTask<RopResult<TCollection>> inputTask)
        where TCollection : IEnumerable<T>
    {
        RopResult<TCollection> previous = await inputTask;
        return new RopResult<IEnumerable<T>>(previous.Result.ToResult<IEnumerable<T>>((_) => (IEnumerable<T>)_), previous.Token);
    }

    /// <summary>
    /// Последовательно применяет шаг к каждому элементу коллекции, накапливая успешные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Накопленные результаты, элемент прерывания и ошибочный результат итерации.</returns>
    private static async Task<(IEnumerable<TOutput>, TInput, Result)> SelectEachAsync<TInput, TOutput>(
    IEnumerable<TInput> items,
    Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> step,
    CancellationToken token)
    {
        var result = new List<TOutput>();
        TInput abortedOn = default;
        Result iterationFailure = Result.Ok();
        foreach (TInput item in items)
        {
            if (token.IsCancellationRequested)
                break;
            Result<TOutput> res = await step(item, token);
            if (res.IsSuccess)
                result.Add(res.Value);
            else
            {
                abortedOn = item;
                iterationFailure = res.ToResult();
                break;
            }
        }
        return (result, abortedOn, iterationFailure);
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

    /// <summary>
    /// Добавляет <see cref="ParametrizedError"/> к ошибкам неуспешного типизированного результата,
    /// если заданы входные данные или метка шага.
    /// </summary>
    /// <typeparam name="TInput">Тип значения результата.</typeparam>
    /// <param name="result">Результат шага.</param>
    /// <param name="label">Метка шага.</param>
    /// <param name="context">Контекст входных данных шага.</param>
    private static Result<TInput> AttachContext<TInput>(Result<TInput> result, string label, object context) =>
        result.IsFailed && HasCallData(label, context)
            ? Result.Fail<TInput>(AppendCallData(result.Errors, label, context))
            : result;

    /// <summary>
    /// Добавляет <see cref="ParametrizedError"/> к ошибкам неуспешного результата,
    /// если заданы входные данные или метка шага.
    /// </summary>
    /// <param name="result">Результат шага.</param>
    /// <param name="label">Метка шага.</param>
    /// <param name="context">Контекст входных данных шага.</param>
    private static Result AttachContext(Result result, string label, object context) =>
        result.IsFailed && HasCallData(label, context)
            ? Result.Fail(AppendCallData(result.Errors, label, context))
            : result;

    /// <summary>
    /// Добавляет <see cref="ParametrizedError"/> к существующим ошибкам без <see cref="Enumerable.Union{TSource}"/>.
    /// </summary>
    /// <param name="errors">Ошибки исходного результата.</param>
    /// <param name="label">Метка шага.</param>
    /// <param name="context">Контекст входных данных шага.</param>
    private static IEnumerable<IError> AppendCallData(IReadOnlyList<IError> errors, string label, object context)
    {
        var callData = new ParametrizedError(label, context);
        int count = errors.Count;
        if (count == 0)
            return [callData];

        if (count == 1)
            return new IError[] { errors[0], callData };

        var merged = new List<IError>(count + 1);
        merged.AddRange(errors);
        merged.Add(callData);
        return merged;
    }

    /// <summary><c>true</c>, если к ошибке шага есть контекст входа или метка для маршрутизации.</summary>
    private static bool HasCallData(string label, object context) =>
        context is not null || label is not null;

    /// <summary>
    /// Последовательно применяет шаг к каждому элементу коллекции и возвращает накопленные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static async ValueTask<Result<IEnumerable<TOutput>>> EachCore<TInput, TOutput>(
    IEnumerable<TInput> source,
    Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> step,
    CancellationToken token)
    {
        (IEnumerable<TOutput> items, TInput abortedOn, Result iterationFailure) = await SelectEachAsync<TInput, TOutput>(source, step, token);
        return (token.IsCancellationRequested, iterationFailure.IsFailed) switch
        {
            (true, _) => FailCancelled<IEnumerable<TOutput>>(token),
            (_, true) => FailSequenceAborted<TInput>(abortedOn, iterationFailure),
            _ => Result.Ok<IEnumerable<TOutput>>(items),
        };
    }

    /// <summary>
    /// Последовательно применяет побочный эффект к каждому элементу коллекции без накопления значений.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="source">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static async ValueTask<Result> EachCoreVoid<TInput>(
        IEnumerable<TInput> source,
        Func<TInput, CancellationToken, ValueTask<Result>> step,
        CancellationToken token)
    {
        TInput abortedOn = default;
        Result iterationFailure = Result.Ok();
        foreach (TInput item in source)
        {
            if (token.IsCancellationRequested)
                break;

            Result stepResult = await step(item, token);
            if (stepResult.IsSuccess)
                continue;

            abortedOn = item;
            iterationFailure = stepResult;
            break;
        }

        return (token.IsCancellationRequested, iterationFailure.IsFailed) switch
        {
            (true, _) => FailCancelled(token),
            (_, true) => FailSequenceAborted(abortedOn, iterationFailure),
            _ => Result.Ok(),
        };
    }

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="CancelledError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult<T> FailCancelled<T>(CancellationToken token) =>
        new(Result.Fail<T>(new CancelledError()), token);

    /// <summary>Возвращает <see cref="RopResult"/> с <see cref="CancelledError"/>.</summary>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult FailCancelled(CancellationToken token) =>
        new(Result.Fail(new CancelledError()), token);

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="NoDataError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult<T> FailNoData<T>(CancellationToken token) =>
        new(Result.Fail<T>(new NoDataError()), token);

    /// <summary>Возвращает <see cref="RopResult"/> с <see cref="NoDataError"/>.</summary>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult FailNoData(CancellationToken token) =>
        new(Result.Fail(new NoDataError()), token);

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="PipelineTerminatedError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult<T> FailPipelineTerminated<T>(CancellationToken token) =>
        new(Result.Fail<T>(new PipelineTerminatedError()), token);

    /// <summary>Возвращает типизированный <see cref="Result"/> с <see cref="SequenceAbortedError{TAbortedOn}"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <typeparam name="TAbortedOn">Тип элемента коллекции, на котором обработка прервана.</typeparam>
    /// <param name="abortedOn">Элемент коллекции, на котором обработка прервана.</param>
    /// <param name="iterationFailure">Ошибочный результат итерации на элементе.</param>
    private static Result<T> FailSequenceAborted<T, TAbortedOn>(TAbortedOn abortedOn, Result iterationFailure) =>
        Result.Fail<T>(new SequenceAbortedError<TAbortedOn>(abortedOn, iterationFailure.Errors));

    /// <summary>Возвращает <see cref="Result"/> с <see cref="SequenceAbortedError{TAbortedOn}"/>.</summary>
    /// <typeparam name="TAbortedOn">Тип элемента коллекции, на котором обработка прервана.</typeparam>
    /// <param name="abortedOn">Элемент коллекции, на котором обработка прервана.</param>
    /// <param name="iterationFailure">Ошибочный результат итерации на элементе.</param>
    private static Result FailSequenceAborted<TAbortedOn>(TAbortedOn abortedOn, Result iterationFailure) =>
        Result.Fail(new SequenceAbortedError<TAbortedOn>(abortedOn, iterationFailure.Errors));

    /// <summary>Пробрасывает существующие ошибки в типизированный <see cref="RopResult"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="errors">Ошибки предыдущего шага.</param>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult<T> FailWithErrors<T>(IEnumerable<IError> errors, CancellationToken token) =>
        new(Result.Fail<T>(errors), token);

    /// <summary>Пробрасывает существующие ошибки в <see cref="RopResult"/>.</summary>
    /// <param name="errors">Ошибки предыдущего шага.</param>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult FailWithErrors(IEnumerable<IError> errors, CancellationToken token) =>
        new(Result.Fail(errors), token);

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="ExceptionalError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="ex">Перехваченное исключение.</param>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult<T> FailExceptional<T>(Exception ex, CancellationToken token) =>
        new(Result.Fail<T>(new ExceptionalError(ex)), token);

    /// <summary>Возвращает <see cref="RopResult"/> с <see cref="ExceptionalError"/>.</summary>
    /// <param name="ex">Перехваченное исключение.</param>
    /// <param name="token">Токен отмены шага.</param>
    private static RopResult FailExceptional(Exception ex, CancellationToken token) =>
        new(Result.Fail(new ExceptionalError(ex)), token);

    /// <summary>
    /// Выбирает актуальный <see cref="CancellationToken"/> из <see cref="OperationCanceledException"/> или запасного значения.
    /// </summary>
    /// <param name="fallback">Токен отмены, известный до исключения.</param>
    /// <param name="oce">Исключение отмены операции.</param>
    private static CancellationToken ResolveToken(CancellationToken fallback, OperationCanceledException oce) =>
        oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : fallback;

    /// <summary>
    /// Выполняет следующий шаг pipeline с типизированным входом и выходом.
    /// </summary>
    /// <typeparam name="TIn">Тип значения предыдущего шага.</typeparam>
    /// <typeparam name="TOut">Тип результата текущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="step">Функция текущего шага.</param>
    private static ValueTask<RopResult<TOut>> ExecutePipelineStep<TIn, TOut>(
        ValueTask<RopResult<TIn>> inputTask,
        Func<RopResult<TIn>, CancellationToken, ValueTask<RopResult<TOut>>> step) =>
        ExecutePipelineStepCore(inputTask, static previous => previous.Token, step, FailCancelled<TOut>, FailExceptional<TOut>);

    /// <summary>
    /// Выполняет следующий шаг pipeline с типизированным входом и побочным эффектом.
    /// </summary>
    /// <typeparam name="TIn">Тип значения предыдущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="step">Функция текущего шага.</param>
    private static ValueTask<RopResult> ExecutePipelineStep<TIn>(
        ValueTask<RopResult<TIn>> inputTask,
        Func<RopResult<TIn>, CancellationToken, ValueTask<RopResult>> step) =>
        ExecutePipelineStepCore(inputTask, static previous => previous.Token, step, FailCancelled, FailExceptional);

    /// <summary>
    /// Выполняет следующий шаг pipeline после шага без значения.
    /// </summary>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="step">Функция текущего шага.</param>
    private static ValueTask<RopResult> ExecutePipelineStep(
        ValueTask<RopResult> inputTask,
        Func<RopResult, CancellationToken, ValueTask<RopResult>> step) =>
        ExecutePipelineStepCore(inputTask, static previous => previous.Token, step, FailCancelled, FailExceptional);

    /// <summary>
    /// Выполняет следующий шаг pipeline после шага без значения с возвратом нового значения.
    /// </summary>
    /// <typeparam name="TOut">Тип результата текущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="step">Функция текущего шага.</param>
    private static ValueTask<RopResult<TOut>> ExecutePipelineStep<TOut>(
        ValueTask<RopResult> inputTask,
        Func<RopResult, CancellationToken, ValueTask<RopResult<TOut>>> step) =>
        ExecutePipelineStepCore(inputTask, static previous => previous.Token, step, FailCancelled<TOut>, FailExceptional<TOut>);

    /// <summary>
    /// Общая обёртка выполнения шага pipeline с перехватом отмены и исключений.
    /// </summary>
    /// <typeparam name="TResult">Тип результата шага.</typeparam>
    /// <typeparam name="TPrevious">Тип результата предыдущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="getToken">Функция извлечения токена отмены из предыдущего результата.</param>
    /// <param name="step">Функция текущего шага.</param>
    /// <param name="failCancelled">Фабрика результата при отмене.</param>
    /// <param name="failExceptional">Фабрика результата при необработанном исключении.</param>
    private static async ValueTask<TResult> ExecutePipelineStepCore<TResult, TPrevious>(
        ValueTask<TPrevious> inputTask,
        Func<TPrevious, CancellationToken> getToken,
        Func<TPrevious, CancellationToken, ValueTask<TResult>> step,
        Func<CancellationToken, TResult> failCancelled,
        Func<Exception, CancellationToken, TResult> failExceptional)
    {
        CancellationToken token = default;
        try
        {
            TPrevious previous = await inputTask;
            token = getToken(previous);
            return await step(previous, token);
        }
        catch (Exception ex)
        {
            return ex switch
            {
                OperationCanceledException oce => failCancelled(ResolveToken(token, oce)),
                _ => failExceptional(ex, token)
            };
        }
    }

    /// <summary>
    /// Выполняет первый шаг pipeline с перехватом отмены и исключений.
    /// </summary>
    /// <param name="action">Функция шага.</param>
    /// <param name="token">Токен отмены.</param>
    private static async ValueTask<RopResult> Guard(
        Func<CancellationToken, ValueTask<RopResult>> action,
        CancellationToken token)
    {
        try
        {
            return await action(token);
        }
        catch (OperationCanceledException)
        {
            return FailCancelled(token);
        }
        catch (Exception ex)
        {
            return FailExceptional(ex, token);
        }
    }

    /// <summary>
    /// Выполняет первый типизированный шаг pipeline с перехватом отмены и исключений.
    /// </summary>
    /// <typeparam name="TInput">Тип значения результата шага.</typeparam>
    /// <param name="action">Функция шага.</param>
    /// <param name="token">Токен отмены.</param>
    private static async ValueTask<RopResult<TInput>> Guard<TInput>(
    Func<CancellationToken, ValueTask<RopResult<TInput>>> action,
    CancellationToken token)
    {
        try
        {
            return await action(token);
        }
        catch (OperationCanceledException)
        {
            return FailCancelled<TInput>(token);
        }
        catch (Exception ex)
        {
            return FailExceptional<TInput>(ex, token);
        }
    }

    /// <summary>Оборачивает <see cref="Task{T}"/> в <see cref="Result.Ok{T}(T)"/> без async-лямбды в адаптерах.</summary>
    private static ValueTask<Result<T>> LiftOk<T>(Task<T> task)
    {
        if (task.IsCompletedSuccessfully)
            return ValueTask.FromResult(Result.Ok(task.Result));
        return LiftOkCore(task);
    }

    private static async ValueTask<Result<T>> LiftOkCore<T>(Task<T> task) => Result.Ok(await task);

    /// <summary>Ожидает void-<see cref="Task"/> и возвращает успешный <see cref="Result"/>.</summary>
    private static ValueTask<Result> LiftVoid(Task task)
    {
        if (task.IsCompletedSuccessfully)
            return ValueTask.FromResult(Result.Ok());
        return LiftVoidCore(task);
    }

    private static async ValueTask<Result> LiftVoidCore(Task task)
    {
        await task;
        return Result.Ok();
    }

    #endregion

}
