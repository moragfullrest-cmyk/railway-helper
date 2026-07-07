using System.Collections;
using System.Linq.Expressions;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Collections

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

    /// <summary>Возвращает <c>true</c>, если вход — массив или <see cref="IReadOnlyList{T}"/> с известным размером.</summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="indexed">Индексируемый источник элементов.</param>
    /// <param name="count">Число элементов.</param>
    private static bool TryGetIndexedSource<TInput>(
        IEnumerable<TInput> items,
        out IReadOnlyList<TInput> indexed,
        out int count)
    {
        switch (items)
        {
            case TInput[] array:
                indexed = array;
                count = array.Length;
                return true;
            case IReadOnlyList<TInput> list:
                indexed = list;
                count = list.Count;
                return true;
            default:
                indexed = default;
                count = 0;
                return false;
        }
    }

    /// <summary>Создаёт список с предварительным размером, если коллекция известна.</summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип элемента результирующего списка.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    private static List<TOutput> CreatePresizedList<TInput, TOutput>(IEnumerable<TInput> items) =>
        items switch
        {
            TInput[] array => new List<TOutput>(array.Length),
            IReadOnlyCollection<TInput> readOnly => new List<TOutput>(readOnly.Count),
            ICollection<TInput> collection => new List<TOutput>(collection.Count),
            _ => new List<TOutput>(),
        };

    /// <summary>
    /// Последовательно применяет шаг к каждому элементу коллекции, накапливая успешные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="step">Асинхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Накопленные результаты, элемент прерывания и ошибочный результат итерации.</returns>
    private static async ValueTask<(IEnumerable<TOutput>, TInput, Result)> SelectEachAsync<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, ValueTask<Result<TOutput>>> step,
        CancellationToken token)
    {
        List<TOutput> result = CreatePresizedList<TInput, TOutput>(items);
        TInput abortedOn = default;
        Result iterationFailure = Result.Ok();
        foreach (TInput item in items)
        {
            if (token.IsCancellationRequested)
                break;

            ValueTask<Result<TOutput>> stepTask = step(item, token);
            Result<TOutput> res = stepTask.IsCompletedSuccessfully ? stepTask.Result : await stepTask;
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
    /// Синхронно применяет шаг к каждому элементу коллекции, накапливая успешные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static (IEnumerable<TOutput>, TInput, Result) SelectEachSync<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, TOutput> step,
        CancellationToken token)
    {
        if (TryGetIndexedSource(items, out IReadOnlyList<TInput> indexed, out int count))
        {
            TOutput[] result = new TOutput[count];
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                    break;
                result[i] = step(indexed[i]);
            }
            return (result, default, Result.Ok());
        }

        List<TOutput> list = CreatePresizedList<TInput, TOutput>(items);
        foreach (TInput item in items)
        {
            if (token.IsCancellationRequested)
                break;
            list.Add(step(item));
        }
        return (list, default, Result.Ok());
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции, накапливая успешные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static (IEnumerable<TOutput>, TInput, Result) SelectEachSync<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, Result<TOutput>> step,
        CancellationToken token)
    {
        if (TryGetIndexedSource(items, out IReadOnlyList<TInput> indexed, out int count))
        {
            TOutput[] result = new TOutput[count];
            TInput abortedOn = default;
            Result iterationFailure = Result.Ok();
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                Result<TOutput> res = step(indexed[i]);
                if (res.IsSuccess)
                    result[i] = res.Value;
                else
                {
                    abortedOn = indexed[i];
                    iterationFailure = res.ToResult();
                    break;
                }
            }
            return (result, abortedOn, iterationFailure);
        }

        List<TOutput> list = CreatePresizedList<TInput, TOutput>(items);
        TInput abortedOnList = default;
        Result iterationFailureList = Result.Ok();
        foreach (TInput item in items)
        {
            if (token.IsCancellationRequested)
                break;

            Result<TOutput> res = step(item);
            if (res.IsSuccess)
                list.Add(res.Value);
            else
            {
                abortedOnList = item;
                iterationFailureList = res.ToResult();
                break;
            }
        }
        return (list, abortedOnList, iterationFailureList);
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции, накапливая успешные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static (IEnumerable<TOutput>, TInput, Result) SelectEachSync<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, TOutput> step,
        CancellationToken token)
    {
        if (TryGetIndexedSource(items, out IReadOnlyList<TInput> indexed, out int count))
        {
            TOutput[] result = new TOutput[count];
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                    break;
                result[i] = step(indexed[i], token);
            }
            return (result, default, Result.Ok());
        }

        List<TOutput> list = CreatePresizedList<TInput, TOutput>(items);
        foreach (TInput item in items)
        {
            if (token.IsCancellationRequested)
                break;
            list.Add(step(item, token));
        }
        return (list, default, Result.Ok());
    }

    /// <summary>
    /// Синхронно применяет шаг к каждому элементу коллекции, накапливая успешные результаты.
    /// </summary>
    /// <typeparam name="TInput">Тип элемента входной коллекции.</typeparam>
    /// <typeparam name="TOutput">Тип результата шага для элемента.</typeparam>
    /// <param name="items">Входная коллекция.</param>
    /// <param name="step">Синхронная функция для элемента.</param>
    /// <param name="token">Токен отмены.</param>
    private static (IEnumerable<TOutput>, TInput, Result) SelectEachSync<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Result<TOutput>> step,
        CancellationToken token)
    {
        if (TryGetIndexedSource(items, out IReadOnlyList<TInput> indexed, out int count))
        {
            TOutput[] result = new TOutput[count];
            TInput abortedOn = default;
            Result iterationFailure = Result.Ok();
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                Result<TOutput> res = step(indexed[i], token);
                if (res.IsSuccess)
                    result[i] = res.Value;
                else
                {
                    abortedOn = indexed[i];
                    iterationFailure = res.ToResult();
                    break;
                }
            }
            return (result, abortedOn, iterationFailure);
        }

        List<TOutput> list = CreatePresizedList<TInput, TOutput>(items);
        TInput abortedOnList = default;
        Result iterationFailureList = Result.Ok();
        foreach (TInput item in items)
        {
            if (token.IsCancellationRequested)
                break;

            Result<TOutput> res = step(item, token);
            if (res.IsSuccess)
                list.Add(res.Value);
            else
            {
                abortedOnList = item;
                iterationFailureList = res.ToResult();
                break;
            }
        }
        return (list, abortedOnList, iterationFailureList);
    }
    #endregion

}
