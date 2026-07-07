using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace RailwayHelper;

public static partial class RailwayHelper
{
    #region Internal - Pipeline

    /// <summary>
    /// Добавляет <see cref="ParametrizedError"/> к ошибкам неуспешного типизированного результата,
    /// если заданы входные данные или метка шага.
    /// </summary>
    /// <typeparam name="TInput">Тип значения результата.</typeparam>
    /// <param name="result">Результат шага.</param>
    /// <param name="label">Метка шага.</param>
    /// <param name="context">Контекст входных данных шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasCallData(string label, object context) =>
        context is not null || label is not null;
    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="CancelledError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult<T> FailCancelled<T>(CancellationToken token) =>
        new(Result.Fail<T>(new CancelledError()), token);

    /// <summary>Возвращает <see cref="RopResult"/> с <see cref="CancelledError"/>.</summary>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult FailCancelled(CancellationToken token) =>
        new(Result.Fail(new CancelledError()), token);

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="NoDataError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult<T> FailNoData<T>(CancellationToken token) =>
        new(Result.Fail<T>(new NoDataError()), token);

    /// <summary>Возвращает <see cref="RopResult"/> с <see cref="NoDataError"/>.</summary>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult FailNoData(CancellationToken token) =>
        new(Result.Fail(new NoDataError()), token);

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="PipelineTerminatedError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult<T> FailWithErrors<T>(IEnumerable<IError> errors, CancellationToken token) =>
        new(Result.Fail<T>(errors), token);

    /// <summary>Пробрасывает существующие ошибки в <see cref="RopResult"/>.</summary>
    /// <param name="errors">Ошибки предыдущего шага.</param>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult FailWithErrors(IEnumerable<IError> errors, CancellationToken token) =>
        new(Result.Fail(errors), token);

    /// <summary>Возвращает типизированный <see cref="RopResult"/> с <see cref="ExceptionalError"/>.</summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <param name="ex">Перехваченное исключение.</param>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult<T> FailExceptional<T>(Exception ex, CancellationToken token) =>
        new(Result.Fail<T>(new ExceptionalError(ex)), token);

    /// <summary>Возвращает <see cref="RopResult"/> с <see cref="ExceptionalError"/>.</summary>
    /// <param name="ex">Перехваченное исключение.</param>
    /// <param name="token">Токен отмены шага.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RopResult FailExceptional(Exception ex, CancellationToken token) =>
        new(Result.Fail(new ExceptionalError(ex)), token);

    /// <summary>
    /// Выбирает актуальный <see cref="CancellationToken"/> из <see cref="OperationCanceledException"/> или запасного значения.
    /// </summary>
    /// <param name="fallback">Токен отмены, известный до исключения.</param>
    /// <param name="oce">Исключение отмены операции.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    private static ValueTask<TResult> ExecutePipelineStepCore<TResult, TPrevious>(
        ValueTask<TPrevious> inputTask,
        Func<TPrevious, CancellationToken> getToken,
        Func<TPrevious, CancellationToken, ValueTask<TResult>> step,
        Func<CancellationToken, TResult> failCancelled,
        Func<Exception, CancellationToken, TResult> failExceptional)
    {
        if (!inputTask.IsCompletedSuccessfully)
            return ExecutePipelineStepCoreSlow(inputTask, getToken, step, failCancelled, failExceptional);

        return ExecutePipelineStepCoreFromCompletedInput(
            inputTask.Result, getToken, step, failCancelled, failExceptional);
    }

    /// <summary>
    /// Выполняет шаг pipeline, когда результат предыдущего шага уже доступен синхронно.
    /// </summary>
    /// <typeparam name="TResult">Тип результата шага.</typeparam>
    /// <typeparam name="TPrevious">Тип результата предыдущего шага.</typeparam>
    /// <param name="previous">Результат предыдущего шага.</param>
    /// <param name="getToken">Функция извлечения токена отмены из предыдущего результата.</param>
    /// <param name="step">Функция текущего шага.</param>
    /// <param name="failCancelled">Фабрика результата при отмене.</param>
    /// <param name="failExceptional">Фабрика результата при необработанном исключении.</param>
    private static ValueTask<TResult> ExecutePipelineStepCoreFromCompletedInput<TResult, TPrevious>(
        TPrevious previous,
        Func<TPrevious, CancellationToken> getToken,
        Func<TPrevious, CancellationToken, ValueTask<TResult>> step,
        Func<CancellationToken, TResult> failCancelled,
        Func<Exception, CancellationToken, TResult> failExceptional)
    {
        CancellationToken token = default;
        try
        {
            token = getToken(previous);
            ValueTask<TResult> stepResult = step(previous, token);
            if (stepResult.IsCompletedSuccessfully)
                return ValueTask.FromResult(stepResult.Result);
            return ExecutePipelineStepCoreAwaitStep(stepResult, token, failCancelled, failExceptional);
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(
                ExecutePipelineStepCoreHandleException(ex, token, failCancelled, failExceptional));
        }
    }

    /// <summary>
    /// Ожидает асинхронный результат шага pipeline с перехватом отмены и исключений.
    /// </summary>
    /// <typeparam name="TResult">Тип результата шага.</typeparam>
    /// <param name="stepResult">Задача текущего шага.</param>
    /// <param name="token">Токен отмены, известный до ожидания.</param>
    /// <param name="failCancelled">Фабрика результата при отмене.</param>
    /// <param name="failExceptional">Фабрика результата при необработанном исключении.</param>
    private static async ValueTask<TResult> ExecutePipelineStepCoreAwaitStep<TResult>(
        ValueTask<TResult> stepResult,
        CancellationToken token,
        Func<CancellationToken, TResult> failCancelled,
        Func<Exception, CancellationToken, TResult> failExceptional)
    {
        try
        {
            return await stepResult;
        }
        catch (Exception ex)
        {
            return ExecutePipelineStepCoreHandleException(ex, token, failCancelled, failExceptional);
        }
    }

    /// <summary>
    /// Выполняет шаг pipeline с ожиданием результата предыдущего шага.
    /// </summary>
    /// <typeparam name="TResult">Тип результата шага.</typeparam>
    /// <typeparam name="TPrevious">Тип результата предыдущего шага.</typeparam>
    /// <param name="inputTask">Задача с результатом предыдущего шага.</param>
    /// <param name="getToken">Функция извлечения токена отмены из предыдущего результата.</param>
    /// <param name="step">Функция текущего шага.</param>
    /// <param name="failCancelled">Фабрика результата при отмене.</param>
    /// <param name="failExceptional">Фабрика результата при необработанном исключении.</param>
    private static async ValueTask<TResult> ExecutePipelineStepCoreSlow<TResult, TPrevious>(
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
            ValueTask<TResult> stepResult = step(previous, token);
            if (stepResult.IsCompletedSuccessfully)
                return stepResult.Result;
            return await stepResult;
        }
        catch (Exception ex)
        {
            return ExecutePipelineStepCoreHandleException(ex, token, failCancelled, failExceptional);
        }
    }

    /// <summary>Преобразует исключение шага pipeline в типизированный результат ошибки.</summary>
    /// <typeparam name="TResult">Тип результата шага.</typeparam>
    /// <param name="ex">Перехваченное исключение.</param>
    /// <param name="token">Токен отмены, известный до исключения.</param>
    /// <param name="failCancelled">Фабрика результата при отмене.</param>
    /// <param name="failExceptional">Фабрика результата при необработанном исключении.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TResult ExecutePipelineStepCoreHandleException<TResult>(
        Exception ex,
        CancellationToken token,
        Func<CancellationToken, TResult> failCancelled,
        Func<Exception, CancellationToken, TResult> failExceptional) =>
        ex switch
        {
            OperationCanceledException oce => failCancelled(ResolveToken(token, oce)),
            _ => failExceptional(ex, token)
        };

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask<Result<T>> LiftOk<T>(Task<T> task)
    {
        if (task.IsCompletedSuccessfully)
            return ValueTask.FromResult(Result.Ok(task.Result));
        return LiftOkCore(task);
    }

    private static async ValueTask<Result<T>> LiftOkCore<T>(Task<T> task) => Result.Ok(await task);

    /// <summary>Ожидает void-<see cref="Task"/> и возвращает успешный <see cref="Result"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
