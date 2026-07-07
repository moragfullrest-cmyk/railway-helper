using System.Collections;
using FluentResults;
using RailwayHelper;
using Shouldly;
using static RailwayHelper.RailwayHelper;

namespace RailwayHelper.Tests;

/// <summary>
/// Юнит-тесты Railway-oriented pipeline (Do / Next / Peek / DoEach / NextEach / PeekEach / OnFailure).
/// </summary>
public sealed class RailwayHelperTests
{
    private const string StepLabel = "step";
    private const string DataLabel = "load";
    private const string DocumentCreationLabel = "Creating Document";
    private const string PlainError = "plain-error";

    public static TheoryData<IEnumerable<int>> EmptyCollections { get; } = new()
    {
        (IEnumerable<int>)null,
        (IEnumerable<int>)Array.Empty<int>(),
        (IEnumerable<int>)new List<int>()
    };

    public static TheoryData<IEnumerable<int>> EmptyOnlyCollections { get; } = new()
    {
        (IEnumerable<int>)Array.Empty<int>(),
        (IEnumerable<int>)new List<int>()
    };

    public static TheoryData<IReadOnlyCollection<int>> EmptyReadOnlyCollections { get; } = new()
    {
        (IReadOnlyCollection<int>)null,
        (IReadOnlyCollection<int>)Array.Empty<int>(),
        (IReadOnlyCollection<int>)new List<int>()
    };

    public static TheoryData<IReadOnlyCollection<int>> EmptyOnlyReadOnlyCollections { get; } = new()
    {
        (IReadOnlyCollection<int>)Array.Empty<int>(),
        (IReadOnlyCollection<int>)new List<int>()
    };

    public static TheoryData<int[]> EmptyArrays { get; } = new()
    {
        (int[])null,
        Array.Empty<int>(),
    };

    public static TheoryData<int[]> EmptyOnlyArrays { get; } = new()
    {
        Array.Empty<int>(),
    };

    #region Do

    [Fact(DisplayName = "Do — синхронная функция возвращает значение")]
    public async Task Do_when_sync_func_returns_value()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 42, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(42);
        #endregion
    }

    [Fact(DisplayName = "Do — async Func<Task<T>> возвращает значение")]
    public async Task Do_when_async_func_returns_value()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(async () => await Task.FromResult(7), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(7);
        #endregion
    }

    [Fact(DisplayName = "Do — Func<Result<T>> с успехом пробрасывает Value")]
    public async Task Do_when_result_func_succeeds_returns_value()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Ok(11), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(11);
        #endregion
    }

    [Fact(DisplayName = "Do — Func<Task<Result<T>>> с ошибкой пробрасывает ошибку")]
    public async Task Do_when_async_result_func_fails_propagates_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(
            async () => await Task.FromResult(Result.Fail<int>("async-err")),
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.Errors[0].Message.ShouldBe("async-err");
        #endregion
    }

    [Fact(DisplayName = "Do — Func<Result<T>> с ошибкой сохраняет исходное сообщение")]
    public async Task Do_when_result_func_fails_preserves_message()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Fail<int>(PlainError), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.Errors[0].Message.ShouldBe(PlainError);
        #endregion
    }

    [Fact(DisplayName = "Do — Action без возврата завершается успешно")]
    public async Task Do_when_action_side_effect_succeeds()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<bool> rop = await Do(() => called = true, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.IsSuccess.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "Do — Func<Task> без возврата завершается успешно")]
    public async Task Do_when_async_action_succeeds()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult rop = await Do(
            async () => { called = true; await Task.CompletedTask; },
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.IsSuccess.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "Do — CancellationToken передаётся в делегат")]
    public async Task Do_when_func_accepts_token_receives_same_token()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        CancellationToken captured = default;
        #endregion

        #region Act
        await Do(
            (CancellationToken t) => { captured = t; return 1; },
            cancellationToken: expected);
        #endregion

        #region Assert
        captured.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "Do — входной параметр передаётся в func")]
    public async Task Do_when_input_provided_passes_to_func()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(9, x => x * 2, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe(18);
        #endregion
    }

    [Fact(DisplayName = "Do — исключение оборачивается в ExceptionalError")]
    public async Task Do_when_exception_wraps_exceptional_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do<int>(
            async () => throw new InvalidOperationException("boom"),
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.Errors[0].ShouldBeOfType<ExceptionalError>();
        #endregion
    }

    [Fact(DisplayName = "Do — label при ошибке добавляет ParametrizedError")]
    public async Task Do_when_fail_with_label_attaches_parametrized_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: StepLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out ParametrizedError ctx).ShouldBeTrue();
        ctx.Label.ShouldBe(StepLabel);
        ctx.Args.ShouldBe(7);
        #endregion
    }

    [Fact(DisplayName = "Do — label при успехе не добавляет ParametrizedError")]
    public async Task Do_when_success_with_label_has_no_parametrized_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, x => x, label: StepLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out _).ShouldBeFalse();
        #endregion
    }

    [Theory(DisplayName = "Do — пустой массив T[] на входе даёт NoDataError")]
    [MemberData(nameof(EmptyArrays))]
    public async Task Do_when_empty_array_input_returns_no_data(int[] input)
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(input, x => x.Length, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "Do — null на входе даёт NoDataError")]
    public async Task Do_when_null_input_returns_no_data()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do((string)null, x => x.Length, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "Do — пустой List<T> на входе даёт NoDataError")]
    public async Task Do_when_empty_list_returns_no_data()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(new List<int>(), x => x.Count, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "Do — IGrouping на входе не падает при проверке непустоты")]
    public async Task Do_when_grouping_input_checks_element_count()
    {
        #region Arrange
        Item[] items = [new Item(1, "a"), new Item(1, "b")];
        IGrouping<int, Item> group = items.GroupBy(x => x.GroupId).Single();
        #endregion

        #region Act
        RopResult<int> rop = await Do(group, g => g.Count(), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(2);
        #endregion
    }

    [Fact(DisplayName = "Do — пустой IGrouping на входе даёт NoDataError")]
    public async Task Do_when_empty_grouping_returns_no_data()
    {
        #region Arrange
        IGrouping<int, Item> group = new TestGrouping<int, Item>(1, Array.Empty<Item>());
        #endregion

        #region Act
        RopResult<int> rop = await Do(group, g => g.Count(), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "Do — пустой non-generic IEnumerable<T> даёт NoDataError")]
    public async Task Do_when_empty_non_generic_enumerable_returns_no_data()
    {
        #region Arrange
        var input = new NonGenericNumbers();
        #endregion

        #region Act
        RopResult<int> rop = await Do(input, x => x.Count(), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "Do — непустой non-generic IEnumerable<T> обрабатывается")]
    public async Task Do_when_non_empty_non_generic_enumerable_succeeds()
    {
        #region Arrange
        var input = new NonGenericNumbers(1, 2, 3);
        #endregion

        #region Act
        RopResult<int> rop = await Do(input, x => x.Sum(), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(6);
        #endregion
    }

    [Fact(DisplayName = "Do — identity overload пробрасывает вход без преобразования")]
    public async Task Do_when_identity_passes_input_through()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(42, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(42);
        #endregion
    }

    [Fact(DisplayName = "Do — side-effect с Result.Fail пробрасывает ошибку")]
    public async Task Do_when_side_effect_returns_fail_propagates_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult rop = await Do(1, _ => Result.Fail("side-err"), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.Errors[0].Message.ShouldBe("side-err");
        #endregion
    }

    [Fact(DisplayName = "Do — отменённый CancellationToken даёт CancelledError")]
    public async Task Do_when_token_already_cancelled_returns_cancelled_error()
    {
        #region Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 42, cancellationToken: cts.Token);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<CancelledError>();
        #endregion
    }

    #endregion

    #region Next

    [Fact(DisplayName = "Next — при успехе предыдущего шага выполняется следующий")]
    public async Task Next_when_previous_succeeded_runs_step()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 2, cancellationToken: TestContext.Current.CancellationToken).Next(x => x + 3);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe(5);
        #endregion
    }

    [Fact(DisplayName = "Next — при ошибке предыдущего шага следующий не выполняется")]
    public async Task Next_when_previous_failed_skips_step()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .Next(x => { called = true; return x; });
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.IsFailed.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "Next — null даёт NoDataError")]
    public async Task Next_when_value_is_null_returns_no_data()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<string> rop = await Do(() => (string)null, cancellationToken: TestContext.Current.CancellationToken).Next(x => x);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Theory(DisplayName = "Next — пустая коллекция обрабатывается штатно")]
    [MemberData(nameof(EmptyOnlyCollections))]
    public async Task Next_when_empty_collection_runs_step(IEnumerable<int> input)
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => input, cancellationToken: TestContext.Current.CancellationToken)
            .Next(x => { called = true; return x.Select(i => i + 1); });
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Theory(DisplayName = "Next — пустая IReadOnlyCollection обрабатывается штатно")]
    [MemberData(nameof(EmptyOnlyReadOnlyCollections))]
    public async Task Next_when_empty_read_only_collection_runs_step(IReadOnlyCollection<int> input)
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => input, cancellationToken: TestContext.Current.CancellationToken)
            .Next(x => { called = true; return x.Select(i => i + 1); });
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Theory(DisplayName = "Next — пустой массив T[] обрабатывается штатно")]
    [MemberData(nameof(EmptyOnlyArrays))]
    public async Task Next_when_empty_array_runs_step(int[] input)
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => input, cancellationToken: TestContext.Current.CancellationToken).Next(x => x.Sum());
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(0);
        #endregion
    }

    [Fact(DisplayName = "Next — пустая строка не считается NoData")]
    public async Task Next_when_empty_string_is_not_no_data()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => string.Empty, cancellationToken: TestContext.Current.CancellationToken).Next(x => x.Length);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(0);
        #endregion
    }

    [Fact(DisplayName = "Next — непустая коллекция обрабатывается")]
    public async Task Next_when_nonempty_collection_runs_step()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => new[] { 1, 2 }, cancellationToken: TestContext.Current.CancellationToken).Next(x => x.Sum());
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe(3);
        #endregion
    }

    [Fact(DisplayName = "Next — исключение оборачивается в ExceptionalError")]
    public async Task Next_when_exception_wraps_exceptional_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: TestContext.Current.CancellationToken)
            .Next<int, int>(async _ => throw new InvalidOperationException("next-boom"));
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<ExceptionalError>();
        #endregion
    }

    [Fact(DisplayName = "Next — исключение сохраняет Token из предыдущего шага")]
    public async Task Next_when_exception_preserves_token_from_previous_step()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: expected)
            .Next<int, int>(async _ => throw new InvalidOperationException("next-boom"));
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<ExceptionalError>();
        rop.Token.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "Next — OnFailure после исключения получает Token из pipeline")]
    public async Task Next_when_exception_on_failure_receives_pipeline_token()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        CancellationToken captured = default;
        #endregion

        #region Act
        await Do(() => 1, cancellationToken: expected)
            .Next<int, int>(async _ => throw new InvalidOperationException("next-boom"))
            .OnFailure(async (_, token) =>
            {
                captured = token;
                await Task.CompletedTask;
            });
        #endregion

        #region Assert
        captured.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "Next — label при ошибке добавляет контекст значения предыдущего шага")]
    public async Task Next_when_fail_with_label_attaches_previous_value_as_context()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 5, cancellationToken: TestContext.Current.CancellationToken)
            .Next(x => Result.Fail<int>("err"), label: StepLabel);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out ParametrizedError ctx).ShouldBeTrue();
        ctx.Label.ShouldBe(StepLabel);
        ctx.Args.ShouldBe(5);
        #endregion
    }

    [Fact(DisplayName = "Next — цепочка Do → Next → Next композирует значения")]
    public async Task Next_when_chained_composes_values()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => new[] { 1, 2 }, cancellationToken: TestContext.Current.CancellationToken)
            .Next(xs => xs.Sum())
            .Next(sum => sum * 10);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe(30);
        #endregion
    }

    [Fact(DisplayName = "Next — untyped после side-effect Do выполняется")]
    public async Task Next_when_untyped_after_side_effect_do_runs()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        await Do(() => { }, cancellationToken: TestContext.Current.CancellationToken).Next(() => called = true);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "Next — untyped после side-effect Do возвращает значение")]
    public async Task Next_when_untyped_after_side_effect_returns_value()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<string> rop = await Do(() => { }, cancellationToken: TestContext.Current.CancellationToken).Next(() => "hello");
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe("hello");
        #endregion
    }

    [Fact(DisplayName = "Next — отменённый CancellationToken даёт CancelledError")]
    public async Task Next_when_token_already_cancelled_returns_cancelled_error()
    {
        #region Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: cts.Token).Next(x => x + 1);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<CancelledError>();
        #endregion
    }

    [Fact(DisplayName = "Next — OperationCanceledException даёт CancelledError")]
    public async Task Next_when_operation_canceled_returns_cancelled_error()
    {
        #region Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: TestContext.Current.CancellationToken)
            .Next<int, int>(async (_, ct) => throw new OperationCanceledException(cts.Token));
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<CancelledError>();
        #endregion
    }

    #endregion

    #region Peek

    [Fact(DisplayName = "Peek — при успехе предыдущего шага значение проходит без изменений")]
    public async Task Peek_when_previous_succeeded_passes_value_through()
    {
        #region Arrange
        int captured = 0;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 5, cancellationToken: TestContext.Current.CancellationToken)
            .Peek(x => captured = x)
            .Next(x => x + 1);
        #endregion

        #region Assert
        captured.ShouldBe(5);
        rop.Result.Value.ShouldBe(6);
        #endregion
    }

    [Fact(DisplayName = "Peek — при ошибке предыдущего шага не выполняется")]
    public async Task Peek_when_previous_failed_skips_step()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .Peek( async (int _) => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.IsFailed.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "Peek — Result.Fail прерывает pipeline")]
    public async Task Peek_when_delegate_returns_fail_fails_pipeline()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: TestContext.Current.CancellationToken)
            .Peek((int _) => Result.Fail("peek-error"), label: StepLabel);
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.TryGetCallData(StepLabel, out ParametrizedError error).ShouldBeTrue();
        error.Args.ShouldBe(1);
        #endregion
    }

    [Fact(DisplayName = "Peek — null даёт NoDataError")]
    public async Task Peek_when_value_is_null_returns_no_data()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<string> rop = await Do(() => (string)null, cancellationToken: TestContext.Current.CancellationToken)
            .Peek((string _) => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "Peek — сохраняет тип в отличие от Next с Action")]
    public async Task Peek_when_action_preserves_typed_pipeline()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 2, cancellationToken: TestContext.Current.CancellationToken)
            .Peek( (int _) => { })
            .Next(x => x * 3);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe(6);
        #endregion
    }

    [Fact(DisplayName = "Peek — Action без использования значения выполняется")]
    public async Task Peek_when_action_without_value_runs()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 3, cancellationToken: TestContext.Current.CancellationToken)
            .Peek(() => called = true);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.Value.ShouldBe(3);
        #endregion
    }

    [Fact(DisplayName = "Peek — Func<Task> без использования значения выполняется")]
    public async Task Peek_when_func_task_without_value_runs()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 3, cancellationToken: TestContext.Current.CancellationToken)
            .Peek(async () =>
            {
                await Task.Yield();
                called = true;
            });
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.Value.ShouldBe(3);
        #endregion
    }

    [Fact(DisplayName = "Peek — пустая строка не считается NoData")]
    public async Task Peek_when_empty_string_is_not_no_data()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => string.Empty, cancellationToken: TestContext.Current.CancellationToken)
            .Peek((string _) => called = true)
            .Next(x => x.Length);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBe(0);
        #endregion
    }

    [Fact(DisplayName = "Peek — CancellationToken передаётся в делегат")]
    public async Task Peek_when_func_accepts_token_receives_same_token()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        CancellationToken captured = default;
        #endregion

        #region Act
        await Do(() => 1, cancellationToken: expected)
            .Peek((_, token) => captured = token);
        #endregion

        #region Assert
        captured.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "Peek — исключение оборачивается в ExceptionalError")]
    public async Task Peek_when_exception_wraps_exceptional_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: TestContext.Current.CancellationToken)
            .Peek<int>(async (int _) => throw new InvalidOperationException("peek-boom"));
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<ExceptionalError>();
        #endregion
    }

    [Fact(DisplayName = "Peek — исключение сохраняет Token из предыдущего шага")]
    public async Task Peek_when_exception_preserves_token_from_previous_step()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: expected)
            .Peek<int>(async (int _) => throw new InvalidOperationException("peek-boom"));
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<ExceptionalError>();
        rop.Token.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "Peek — label при ошибке добавляет контекст значения предыдущего шага")]
    public async Task Peek_when_fail_with_label_attaches_previous_value_as_context()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 5, cancellationToken: TestContext.Current.CancellationToken)
            .Peek((int x) => Result.Fail("err"), label: StepLabel);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out ParametrizedError ctx).ShouldBeTrue();
        ctx.Label.ShouldBe(StepLabel);
        ctx.Args.ShouldBe(5);
        #endregion
    }

    [Fact(DisplayName = "Peek — отменённый CancellationToken даёт CancelledError")]
    public async Task Peek_when_token_already_cancelled_returns_cancelled_error()
    {
        #region Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: cts.Token)
            .Peek((int x) => { x = x + 1; });
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<CancelledError>();
        #endregion
    }

    [Theory(DisplayName = "PeekEach — пустая коллекция обрабатывается штатно")]
    [MemberData(nameof(EmptyOnlyCollections))]
    public async Task PeekEach_when_empty_collection_runs_without_calls(IEnumerable<int> input)
    {
        #region Arrange
        int callCount = 0;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => input, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(_ => callCount++);
        #endregion

        #region Assert
        callCount.ShouldBe(0);
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Fact(DisplayName = "PeekEach — коллекция проходит без изменений")]
    public async Task PeekEach_when_all_ok_passes_collection_through()
    {
        #region Arrange
        List<int> touched = [];
        int[] input = [1, 2, 3];
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)input, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(x => touched.Add(x));
        #endregion

        #region Assert
        touched.ShouldBe([1, 2, 3]);
        rop.Result.Value.ShouldBe(input);
        #endregion
    }

    [Fact(DisplayName = "PeekEach — сбой на элементе даёт SequenceAbortedError")]
    public async Task PeekEach_when_item_fails_returns_sequence_aborted()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)new[] { 1, 2, 3 }, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(x => x == 2 ? Result.Fail("stop") : Result.Ok());
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.Errors.ShouldContain(e => e is SequenceAbortedError<int> && (e as SequenceAbortedError<int>).AbortedOn == 2);
        rop.Result.Errors[0].ShouldBeOfType<SequenceAbortedError<int>>().Reasons.ShouldContain(e => e.Message == "stop");
        #endregion
    }

    [Fact(DisplayName = "PeekEach — label при сбое добавляет контекст входной коллекции")]
    public async Task PeekEach_when_fail_with_label_attaches_input_collection_context()
    {
        #region Arrange
        int[] input = [1, 2, 3];
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)input, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(x => x == 2 ? Result.Fail("stop") : Result.Ok(), label: StepLabel);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(StepLabel, out ParametrizedError ctx).ShouldBeTrue();
        ctx.Args.ShouldBeSameAs(input);
        #endregion
    }

    [Fact(DisplayName = "PeekEach — сбой на первом элементе прерывает последовательность")]
    public async Task PeekEach_when_first_item_fails_aborts_immediately()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>) new[] { 1, 2, 3 }, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(x => x == 1 ? Result.Fail("stop") : Result.Ok());
        #endregion

        #region Assert
        SequenceAbortedError<int> error = rop.Result.Errors[0].ShouldBeOfType<SequenceAbortedError<int>>();
        error.AbortedOn.ShouldBe(1);
        error.Reasons.ShouldContain(e => e.Message == "stop");
        #endregion
    }

    [Theory(DisplayName = "PeekEach — пустая IReadOnlyCollection обрабатывается штатно")]
    [MemberData(nameof(EmptyOnlyReadOnlyCollections))]
    public async Task PeekEach_when_empty_read_only_collection_runs_without_calls(IReadOnlyCollection<int> input)
    {
        #region Arrange
        int callCount = 0;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => input, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(_ => callCount++);
        #endregion

        #region Assert
        callCount.ShouldBe(0);
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Fact(DisplayName = "PeekEach — IReadOnlyCollection из предыдущего шага без каста")]
    public async Task PeekEach_when_read_only_collection_peeks_items()
    {
        #region Arrange
        List<int> touched = [];
        IReadOnlyCollection<int> items = new[] { 1, 2 };
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(x => touched.Add(x));
        #endregion

        #region Assert
        touched.ShouldBe([1, 2]);
        rop.Result.Value.ShouldBe(items);
        #endregion
    }

    [Fact(DisplayName = "PeekEach — ICollection из предыдущего шага итерируется")]
    public async Task PeekEach_when_icollection_peeks_items()
    {
        #region Arrange
        List<int> touched = [];
        ICollection<int> items = new List<int> { 1, 2 };
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken)
            .PeekEach(x => touched.Add(x));
        #endregion

        #region Assert
        touched.ShouldBe([1, 2]);
        rop.Result.Value.ShouldBe(items);
        #endregion
    }

    #endregion

    #region DoEach / NextEach

    [Fact(DisplayName = "DoEach — все элементы обрабатываются в порядке входа")]
    public async Task DoEach_when_all_ok_returns_mapped_values()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(new[] { 1, 2, 3 }, x => x * 2, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([2, 4, 6]);
        #endregion
    }

    [Fact(DisplayName = "DoEach — null вход даёт NoDataError")]
    public async Task DoEach_when_input_null_returns_no_data()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach((int[])null, x => x, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "DoEach — пустая коллекция даёт NoDataError")]
    public async Task DoEach_when_empty_returns_no_data()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(Array.Empty<int>(), x => x, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "DoEach — сбой на элементе даёт SequenceAbortedError")]
    public async Task DoEach_when_item_fails_aborts_on_element()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(
            new[] { 1, 2, 3 },
            x => x == 2 ? Result.Fail<int>("fail") : Result.Ok(x),
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        SequenceAbortedError<int> error = rop.Result.Errors[0].ShouldBeOfType<SequenceAbortedError<int>>();
        error.AbortedOn.ShouldBe(2);
        error.Reasons.ShouldContain(e => e.Message == "fail");
        #endregion
    }

    [Fact(DisplayName = "DoEach — label при сбое добавляет контекст входной коллекции")]
    public async Task DoEach_when_fail_with_label_attaches_input_collection_context()
    {
        #region Arrange
        int[] input = [1, 2, 3];
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(
            input,
            x => x == 2 ? Result.Fail<int>("fail") : Result.Ok(x),
            label: StepLabel,
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(StepLabel, out ParametrizedError ctx).ShouldBeTrue();
        ctx.Args.ShouldBeSameAs(input);
        #endregion
    }

    [Fact(DisplayName = "DoEach — сбой на первом элементе прерывает последовательность")]
    public async Task DoEach_when_first_item_fails_aborts_immediately()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(
            new[] { 1, 2, 3 },
            x => x == 1 ? Result.Fail<int>("fail") : Result.Ok(x),
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        SequenceAbortedError<int> error = rop.Result.Errors[0].ShouldBeOfType<SequenceAbortedError<int>>();
        error.AbortedOn.ShouldBe(1);
        error.Reasons.ShouldContain(e => e.Message == "fail");
        #endregion
    }

    [Fact(DisplayName = "DoEach — side-effect обрабатывает все элементы")]
    public async Task DoEach_when_side_effect_processes_all_items()
    {
        #region Arrange
        int sum = 0;
        #endregion

        #region Act
        RopResult rop = (await DoEach(new[] { 1, 2, 3 }, (int x) => sum += x, cancellationToken: TestContext.Current.CancellationToken)).ToUntyped();
        #endregion

        #region Assert
        sum.ShouldBe(6);
        rop.Result.IsSuccess.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "DoEach — async-делегат на элемент обрабатывается корректно")]
    public async Task DoEach_when_async_step_returns_values()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(
            new[] { 1, 2 },
            async x => await Task.FromResult(x * 3),
            cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([3, 6]);
        #endregion
    }

    [Fact(DisplayName = "NextEach — итерирует коллекцию из предыдущего шага")]
    public async Task NextEach_when_previous_has_collection_iterates_items()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)new[] { 1, 2 }, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(x => x + 10);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([11, 12]);
        #endregion
    }

    [Fact(DisplayName = "NextEach — IReadOnlyCollection из предыдущего шага без каста")]
    public async Task NextEach_when_previous_has_read_only_collection_iterates_items()
    {
        #region Arrange
        IReadOnlyCollection<int> items = new[] { 1, 2 };
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken).NextEach(x => x + 10);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([11, 12]);
        #endregion
    }

    [Fact(DisplayName = "NextEach — пустая коллекция на предыдущем шаге завершается без итераций")]
    public async Task NextEach_when_previous_empty_collection_does_not_iterate()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)Array.Empty<int>(), cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(x => { called = true; return x; });
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Fact(DisplayName = "NextEach — пустая IReadOnlyCollection на предыдущем шаге завершается без итераций")]
    public async Task NextEach_when_previous_read_only_collection_empty_does_not_iterate()
    {
        #region Arrange
        bool called = false;
        IReadOnlyCollection<int> items = Array.Empty<int>();
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(x => { called = true; return x; });
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Fact(DisplayName = "DoEach — отменённый CancellationToken даёт CancelledError")]
    public async Task DoEach_when_token_already_cancelled_returns_cancelled_error()
    {
        #region Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(new[] { 1, 2 }, x => x, cancellationToken: cts.Token);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<CancelledError>();
        #endregion
    }

    [Fact(DisplayName = "DoEach — отмена между элементами даёт CancelledError")]
    public async Task DoEach_when_cancelled_mid_iteration_returns_cancelled_error()
    {
        #region Arrange
        using CancellationTokenSource cts = new();
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await DoEach(
            new[] { 1, 2, 3 },
            (int x, CancellationToken _) =>
            {
                if (x == 2)
                    cts.Cancel();
                return Result.Ok(x);
            },
            cancellationToken: cts.Token);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<CancelledError>();
        #endregion
    }

    [Fact(DisplayName = "NextEach — ICollection из предыдущего шага итерируется")]
    public async Task NextEach_when_previous_has_icollection_iterates_items()
    {
        #region Arrange
        ICollection<int> items = new List<int> { 1, 2 };
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(x => x + 10);
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([11, 12]);
        #endregion
    }

    [Fact(DisplayName = "NextEach — side-effect сбой на элементе даёт SequenceAbortedError")]
    public async Task NextEach_when_side_effect_item_fails_aborts_on_element()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult rop = await Do(() => (IEnumerable<int>)new[] { 1, 2, 3 }, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(x => x == 2 ? Result.Fail("fail") : Result.Ok());
        #endregion

        #region Assert
        SequenceAbortedError<int> error = rop.Result.Errors[0].ShouldBeOfType<SequenceAbortedError<int>>();
        error.AbortedOn.ShouldBe(2);
        error.Reasons.ShouldContain(e => e.Message == "fail");
        #endregion
    }

    [Fact(DisplayName = "NextEach — label при сбое добавляет контекст предыдущей коллекции")]
    public async Task NextEach_when_fail_with_label_attaches_previous_collection_context()
    {
        #region Arrange
        int[] input = [1, 2, 3];
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)input, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(
                x => x == 2 ? Result.Fail<int>("fail") : Result.Ok(x),
                label: StepLabel);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(StepLabel, out ParametrizedError ctx).ShouldBeTrue();
        ctx.Args.ShouldBeSameAs(input);
        #endregion
    }

    #endregion

    #region OnFailure

    [Fact(DisplayName = "OnFailure — Action вызывается при ошибке")]
    public async Task OnFailure_when_failed_invokes_action_handler()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        Result<int> result = await Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(_ => called = true);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<HandledFailureError>();
        #endregion
    }

    [Fact(DisplayName = "OnFailure — Func<Task> вызывается при ошибке")]
    public async Task OnFailure_when_failed_invokes_async_handler()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        await Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(async _ => { called = true; await Task.CompletedTask; });
        #endregion

        #region Assert
        called.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "OnFailure — Func с CancellationToken получает Token из RopResult")]
    public async Task OnFailure_when_failed_passes_rop_token_to_handler()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        CancellationToken captured = default;
        #endregion

        #region Act
        await Do(() => Result.Fail<int>("x"), cancellationToken: expected)
            .OnFailure(async (_, token) =>
            {
                captured = token;
                await Task.CompletedTask;
            });
        #endregion

        #region Assert
        captured.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "OnFailure — после handler возвращает HandledFailureError")]
    public async Task OnFailure_when_failed_returns_handled_failure_error()
    {
        #region Arrange
        #endregion

        #region Act
        Result<int> result = await Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(_ => { });
        #endregion

        #region Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<HandledFailureError>();
        #endregion
    }

    [Fact(DisplayName = "OnFailure — при успехе handler не вызывается")]
    public async Task OnFailure_when_success_skips_handler()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        Result<int> result = await Do(() => 1, cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(_ => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        result.Value.ShouldBe(1);
        #endregion
    }

    [Fact(DisplayName = "OnFailure — untyped цепочка вызывает handler при ошибке")]
    public async Task OnFailure_when_untyped_chain_fails_invokes_handler()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        await Do(() => Result.Fail("err"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(_ => called = true);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "OnFailure — untyped Func с CancellationToken получает Token из RopResult")]
    public async Task OnFailure_when_untyped_passes_rop_token_to_handler()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        CancellationToken captured = default;
        #endregion

        #region Act
        await Do(() => Result.Fail("err"), cancellationToken: expected)
            .OnFailure(async (_, token) =>
            {
                captured = token;
                await Task.CompletedTask;
            });
        #endregion

        #region Assert
        captured.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "OnFailure — null Action handler кидает ArgumentNullException")]
    public async Task OnFailure_when_action_handler_is_null_throws_argument_null_exception()
    {
        #region Arrange
        #endregion

        #region Act
        Task<Result<int>> action() => Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure((Action<ResultBase>)null);
        #endregion

        #region Assert
        await Should.ThrowAsync<ArgumentNullException>(action);
        #endregion
    }

    [Fact(DisplayName = "OnFailure — null async handler кидает ArgumentNullException")]
    public async Task OnFailure_when_async_handler_is_null_throws_argument_null_exception()
    {
        #region Arrange
        #endregion

        #region Act
        Task<Result<int>> action() => Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure((Func<ResultBase, Task>)null);
        #endregion

        #region Assert
        await Should.ThrowAsync<ArgumentNullException>(action);
        #endregion
    }

    [Fact(DisplayName = "OnFailure — null handler с token кидает ArgumentNullException")]
    public async Task OnFailure_when_token_handler_is_null_throws_argument_null_exception()
    {
        #region Arrange
        #endregion

        #region Act
        Task<Result<int>> action() => Do(() => Result.Fail<int>("x"), cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure((Func<ResultBase, CancellationToken, Task>)null);
        #endregion

        #region Assert
        await Should.ThrowAsync<ArgumentNullException>(action);
        #endregion
    }

    #endregion

    #region TryGetCallData и типы ошибок

    [Fact(DisplayName = "TryGetCallData — извлекает ParametrizedError с Label и Args")]
    public async Task TryGetCallData_when_labeled_fail_returns_context()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<string> rop = await Do("ctx", _ => Result.Fail<string>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out ParametrizedError error).ShouldBeTrue();
        error.Label.ShouldBe(DataLabel);
        error.Args.ShouldBe("ctx");
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — извлекает последний ParametrizedError")]
    public async Task TryGetCallData_when_multiple_errors_returns_last_parametrized()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(3, _ => Result.Fail<int>("err"), label: StepLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.Errors.Count.ShouldBeGreaterThan(1);
        rop.Result.TryGetCallData(out ParametrizedError error).ShouldBeTrue();
        error.Label.ShouldBe(StepLabel);
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — по label возвращает true при совпадении метки")]
    public async Task TryGetCallData_when_label_matches_returns_context()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(DataLabel, out ParametrizedError error).ShouldBeTrue();
        error.Label.ShouldBe(DataLabel);
        error.Args.ShouldBe(7);
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — по label возвращает false при несовпадении метки")]
    public async Task TryGetCallData_when_label_mismatches_returns_false()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData("other-label", out _).ShouldBeFalse();
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData<TArgs> — извлекает типизированный контекст по label")]
    public async Task TryGetCallData_when_typed_label_matches_returns_args()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<string> rop = await Do("ctx", _ => Result.Fail<string>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData<string>(DataLabel, out string args).ShouldBeTrue();
        args.ShouldBe("ctx");
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData<TArgs> — возвращает false при неверном типе Args")]
    public async Task TryGetCallData_when_typed_args_mismatch_returns_false()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<string> rop = await Do("ctx", _ => Result.Fail<string>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData<int>(DataLabel, out _).ShouldBeFalse();
        #endregion
    }

    [Fact(DisplayName = "WhenCallData — вызывает handler при совпадении label")]
    public async Task WhenCallData_when_label_matches_invokes_action()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        rop.Result.WhenCallData(DataLabel, _ => called = true);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "WhenCallData — не вызывает handler при несовпадении label")]
    public async Task WhenCallData_when_label_mismatches_skips_action()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        rop.Result.WhenCallData("other-label", _ => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        #endregion
    }

    [Fact(DisplayName = "WhenCallData — передаёт ParametrizedError и ResultBase")]
    public async Task WhenCallData_when_context_handler_receives_error_and_result()
    {
        #region Arrange
        string capturedLabel = null;
        object capturedArgs = null;
        ResultBase capturedResult = null;
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        rop.Result.WhenCallData(DataLabel, (ctx, r) =>
        {
            capturedLabel = ctx.Label;
            capturedArgs = ctx.Args;
            capturedResult = r;
        });
        #endregion

        #region Assert
        capturedLabel.ShouldBe(DataLabel);
        capturedArgs.ShouldBe(7);
        capturedResult.ShouldBeSameAs(rop.Result);
        #endregion
    }

    [Fact(DisplayName = "WhenCallData<TArgs> — передаёт типизированный контекст")]
    public async Task WhenCallData_when_typed_handler_receives_args()
    {
        #region Arrange
        int captured = 0;
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        rop.Result.WhenCallData<int>(DataLabel, (args, _) => captured = args);
        #endregion

        #region Assert
        captured.ShouldBe(7);
        #endregion
    }

    [Fact(DisplayName = "WhenCallData — в OnFailure обрабатывает только совпавший label")]
    public async Task WhenCallData_when_used_in_on_failure_invokes_matching_handler_only()
    {
        #region Arrange
        bool dataLoadingCalled = false;
        bool documentCreationCalled = false;
        #endregion

        #region Act
        await Do(7, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(result =>
            {
                result.WhenCallData(DataLabel, _ => dataLoadingCalled = true);
                result.WhenCallData(DocumentCreationLabel, _ => documentCreationCalled = true);
            });
        #endregion

        #region Assert
        dataLoadingCalled.ShouldBeTrue();
        documentCreationCalled.ShouldBeFalse();
        #endregion
    }

    [Fact(DisplayName = "WhenCallData — в OnFailure после handler возвращает HandledFailureError")]
    public async Task WhenCallData_when_used_in_on_failure_returns_handled_failure_error()
    {
        #region Arrange
        #endregion

        #region Act
        Result<int> result = await Do(10, _ => Result.Fail<int>("err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(r => r.WhenCallData(DataLabel, _ => { }));
        #endregion

        #region Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<HandledFailureError>();
        #endregion
    }

    [Fact(DisplayName = "CancelledError — Message равен Canceled")]
    public void CancelledError_has_expected_message()
    {
        #region Arrange
        #endregion

        #region Act
        CancelledError error = new();
        #endregion

        #region Assert
        error.Message.ShouldBe("Canceled");
        #endregion
    }

    [Fact(DisplayName = "NoDataError — Message равен NoData")]
    public void NoDataError_has_expected_message()
    {
        #region Arrange
        #endregion

        #region Act
        NoDataError error = new();
        #endregion

        #region Assert
        error.Message.ShouldBe("NoData");
        #endregion
    }

    [Fact(DisplayName = "SequenceAbortedError — Message, AbortedOn и Reasons")]
    public void SequenceAbortedError_has_expected_message_and_aborted_on()
    {
        #region Arrange
        Error cause = new Error("cause");
        #endregion

        #region Act
        SequenceAbortedError<int> error = new(42, [cause]);
        #endregion

        #region Assert
        error.Message.ShouldBe("SequenceAborted");
        error.AbortedOn.ShouldBe(42);
        error.Reasons.ShouldBe([cause]);
        #endregion
    }

    [Fact(DisplayName = "PipelineTerminatedError — Message равен PipelineTerminated")]
    public void PipelineTerminatedError_has_expected_message()
    {
        #region Arrange
        #endregion

        #region Act
        PipelineTerminatedError error = new();
        #endregion

        #region Assert
        error.Message.ShouldBe("PipelineTerminated");
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — при успехе возвращает false")]
    public async Task TryGetCallData_when_success_returns_false()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out _).ShouldBeFalse();
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — при ошибке без входа и label возвращает false")]
    public async Task TryGetCallData_when_handler_fail_without_input_or_label_returns_false()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Fail<int>(PlainError), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out _).ShouldBeFalse();
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — при ошибке без label, но с входом возвращает Args")]
    public async Task TryGetCallData_when_fail_without_label_but_with_input_returns_args()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(7, _ => Result.Fail<int>(PlainError), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.TryGetCallData(out ParametrizedError error).ShouldBeTrue();
        error.Label.ShouldBeNull();
        error.Args.ShouldBe(7);
        #endregion
    }

    [Fact(DisplayName = "WhenCallData — срабатывает при label без Args")]
    public async Task WhenCallData_when_label_only_without_args_invokes_handler()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Fail<int>(PlainError), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken);
        rop.Result.WhenCallData(DataLabel, _ => called = true);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        rop.Result.TryGetCallData(out _).ShouldBeFalse();
        rop.Result.TryGetCallData(DataLabel, out ParametrizedError error).ShouldBeTrue();
        error.Args.ShouldBeNull();
        #endregion
    }

    [Fact(DisplayName = "TryGetCallData — при инфраструктурной ошибке без ParametrizedError возвращает false")]
    public async Task TryGetCallData_when_no_parametrized_error_returns_false()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<string> rop = await Do(() => (string)null, cancellationToken: TestContext.Current.CancellationToken)
            .Next(x => x);
        #endregion

        #region Assert
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        rop.Result.TryGetCallData(out _).ShouldBeFalse();
        #endregion
    }

    #endregion

    #region RopResult

    [Fact(DisplayName = "RopResult — implicit conversion в Result<T>")]
    public async Task RopResult_when_awaited_converts_to_result()
    {
        #region Arrange
        #endregion

        #region Act
        Result<int> result = await Do(() => 42, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        result.Value.ShouldBe(42);
        #endregion
    }

    [Fact(DisplayName = "RopResult — ToUntyped / FromUntyped сохраняют Token")]
    public async Task RopResult_when_untyped_roundtrip_preserves_token()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: expected);
        RopResult untyped = rop.ToUntyped<int>();
        RopResult<int> retyped = untyped.FromUntyped<int>();
        #endregion

        #region Assert
        retyped.Token.ShouldBe(expected);
        retyped.Result.IsSuccess.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "RopResult — Token совпадает с переданным CancellationToken")]
    public async Task RopResult_when_do_succeeds_token_matches_input()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 1, cancellationToken: expected);
        #endregion

        #region Assert
        rop.Token.ShouldBe(expected);
        #endregion
    }

    [Fact(DisplayName = "RopResult — implicit conversion untyped в Result")]
    public async Task RopResult_when_untyped_converts_to_result()
    {
        #region Arrange
        #endregion

        #region Act
        Result result = await Do(() => { }, cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        result.IsSuccess.ShouldBeTrue();
        #endregion
    }

    #endregion

    #region Интеграционные сценарии

    [Fact(DisplayName = "Pipeline — Do → Peek → Next сохраняет тип и значение")]
    public async Task pipeline_do_peek_next_preserves_value()
    {
        #region Arrange
        List<int> logged = [];
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => 4, cancellationToken: TestContext.Current.CancellationToken)
            .Peek(x => logged.Add(x))
            .Next(x => x * 5);
        #endregion

        #region Assert
        logged.ShouldBe([4]);
        rop.Result.Value.ShouldBe(20);
        #endregion
    }

    [Fact(DisplayName = "Pipeline — Do → Next(group) → PeekEach → NextEach")]
    public async Task pipeline_do_group_peek_each_next_each_succeeds()
    {
        #region Arrange
        Item[] items =
        [
            new Item(1, "a"),
            new Item(1, "b"),
            new Item(2, "c")
        ];
        List<int> peeked = [];
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken)
            .Next(xs => xs.GroupBy(x => x.GroupId))
            .PeekEach(g => peeked.Add(g.Key))
            .NextEach(g => g.Count());
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.OrderBy(x => x).ShouldBe([1, 2]);
        peeked.OrderBy(x => x).ShouldBe([1, 2]);
        #endregion
    }

    [Fact(DisplayName = "Pipeline — Do → Next(group) → NextEach")]
    public async Task pipeline_do_group_next_each_succeeds()
    {
        #region Arrange
        Item[] items =
        [
            new Item(1, "a"),
            new Item(1, "b"),
            new Item(2, "c")
        ];
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(() => items, cancellationToken: TestContext.Current.CancellationToken)
            .Next(xs => xs.GroupBy(x => x.GroupId))
            .NextEach(g => g.Count());
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.OrderBy(x => x).ShouldBe([1, 2]);
        #endregion
    }

    [Fact(DisplayName = "Pipeline — Do → Next → OnFailure + WhenCallData для логирования по label")]
    public async Task pipeline_on_failure_when_call_data_reads_label()
    {
        #region Arrange
        string capturedLabel = null;
        #endregion

        #region Act
        await Do(10, _ => Result.Fail<int>("api-err"), label: DataLabel, cancellationToken: TestContext.Current.CancellationToken)
            .OnFailure(r => r.WhenCallData(DataLabel, (ctx, _) => capturedLabel = ctx.Label));
        #endregion

        #region Assert
        capturedLabel.ShouldBe(DataLabel);
        #endregion
    }

    [Fact(DisplayName = "Pipeline — вложенные DoEach / NextEach")]
    public async Task pipeline_nested_each_succeeds()
    {
        #region Arrange
        int[] orgs = [1, 2];
        Dictionary<int, int[]> docsByOrg = new()
        {
            [1] = [10, 11],
            [2] = [20]
        };
        List<int> processed = [];
        #endregion

        #region Act
        await DoEach(orgs, org => org, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(async org =>
            {
                await DoEach(docsByOrg[org], doc =>
                {
                    processed.Add(doc);
                    return doc;
                }, cancellationToken: TestContext.Current.CancellationToken);
            });
        #endregion

        #region Assert
        processed.OrderBy(x => x).ShouldBe([10, 11, 20]);
        #endregion
    }

    [Fact(DisplayName = "Pipeline — вложенный Do с label пробрасывает контекст в OnFailure")]
    public async Task pipeline_nested_do_failure_preserves_label_in_on_failure()
    {
        #region Arrange
        string capturedLabel = null;
        object capturedArgs = null;
        #endregion

        #region Act
        await DoEach(new[] { 1 }, org => org, cancellationToken: TestContext.Current.CancellationToken)
            .NextEach(async org =>
            {
                await Do(org, _ => Result.Fail<int>("inner"), label: DocumentCreationLabel, cancellationToken: TestContext.Current.CancellationToken)
                    .OnFailure(r => r.WhenCallData(DocumentCreationLabel, (ctx, _) =>
                    {
                        capturedLabel = ctx.Label;
                        capturedArgs = ctx.Args;
                    }));
            });
        #endregion

        #region Assert
        capturedLabel.ShouldBe(DocumentCreationLabel);
        capturedArgs.ShouldBe(1);
        #endregion
    }

    #endregion

    #region IfNoData

    [Fact(DisplayName = "IfNoData — мягкий пропуск: Next выполняется на Empty")]
    public async Task IfNoData_when_handler_does_not_terminate_continues_with_empty()
    {
        #region Arrange
        bool nextCalled = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => { })
            .Next(xs => { nextCalled = true; return xs; });
        #endregion

        #region Assert
        nextCalled.ShouldBeTrue();
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeEmpty();
        #endregion
    }

    [Fact(DisplayName = "IfNoData — возвращённая коллекция подставляется в pipeline")]
    public async Task IfNoData_when_handler_returns_collection_uses_it()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(async _ => new[] { 1, 2, 3 })
            .Next(xs => xs.Select(x => x * 10));
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([10, 20, 30]);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — сохраняет коллекцию входа при side-effect")]
    public async Task IfNoData_when_input_is_array_preserves_array_type()
    {
        #region Arrange
        int[] input = Array.Empty<int>();
        #endregion

        #region Act
        RopResult<int[]> rop = await Do(
                () => input,
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => { });
        #endregion

        #region Assert
        rop.Result.Value.ShouldBeSameAs(input);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — Terminate прерывает pipeline")]
    public async Task IfNoData_when_terminate_skips_next()
    {
        #region Arrange
        bool nextCalled = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(ctx => ctx.Terminate())
            .Next(xs => { nextCalled = true; return xs; });
        #endregion

        #region Assert
        nextCalled.ShouldBeFalse();
        rop.Result.Errors[0].ShouldBeOfType<PipelineTerminatedError>();
        #endregion
    }

    [Fact(DisplayName = "IfNoData — onNoData вызывается при пустой коллекции")]
    public async Task IfNoData_when_empty_invokes_on_no_data()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        await Do(() => (IEnumerable<int>)Array.Empty<int>(), cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => called = true)
            .Next(xs => xs);
        #endregion

        #region Assert
        called.ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "IfNoData — непустая коллекция не вызывает onNoData")]
    public async Task IfNoData_when_has_data_skips_on_no_data()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)new[] { 1, 2 },
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.Value.ShouldBe([1, 2]);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — null даёт NoDataError")]
    public async Task IfNoData_when_null_returns_no_data()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)null,
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.Errors[0].ShouldBeOfType<NoDataError>();
        #endregion
    }

    [Fact(DisplayName = "IsPipelineTerminated — true для PipelineTerminatedError")]
    public async Task IsPipelineTerminated_when_terminated_returns_true()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(ctx => ctx.Terminate());
        #endregion

        #region Assert
        rop.Result.IsPipelineTerminated().ShouldBeTrue();
        #endregion
    }

    [Fact(DisplayName = "IfNoData — Terminate имеет приоритет над возвращённой коллекцией")]
    public async Task IfNoData_when_terminate_after_return_still_terminates()
    {
        #region Arrange
        bool nextCalled = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(async ctx =>
            {
                ctx.Terminate();
                return new[] { 1, 2, 3 };
            })
            .Next(xs => { nextCalled = true; return xs; });
        #endregion

        #region Assert
        nextCalled.ShouldBeFalse();
        rop.Result.Errors[0].ShouldBeOfType<PipelineTerminatedError>();
        #endregion
    }

    [Fact(DisplayName = "IfNoData — async handler возвращает коллекцию")]
    public async Task IfNoData_when_async_handler_returns_collection()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(async ctx =>
            {
                await Task.Delay(1, ctx.Token);
                return new[] { 4, 5 };
            })
            .Next(xs => xs.Select(x => x * 2));
        #endregion

        #region Assert
        rop.Result.Value.ShouldBe([8, 10]);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — async onNoData с CancellationToken получает token контекста")]
    public async Task IfNoData_when_async_with_token_receives_context_token()
    {
        #region Arrange
        CancellationToken expected = TestContext.Current.CancellationToken;
        CancellationToken captured = default;
        int[] input = Array.Empty<int>();
        #endregion

        #region Act
        RopResult<int[]> rop = await Do(
                () => input,
                cancellationToken: expected)
            .IfNoData(async (ctx, token) =>
            {
                captured = token;
                await Task.CompletedTask;
            });
        #endregion

        #region Assert
        captured.ShouldBe(expected);
        rop.Result.Value.ShouldBeSameAs(input);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — handler с Result.Fail пробрасывает ошибку")]
    public async Task IfNoData_when_handler_returns_fail_propagates_error()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => Result.Fail("handler-err"));
        #endregion

        #region Assert
        rop.Result.IsFailed.ShouldBeTrue();
        rop.Result.Errors[0].Message.ShouldBe("handler-err");
        #endregion
    }

    [Fact(DisplayName = "IfNoData — при ошибке предыдущего шага handler не вызывается")]
    public async Task IfNoData_when_previous_failed_skips_handler()
    {
        #region Arrange
        bool called = false;
        #endregion

        #region Act
        RopResult<IEnumerable<int>> rop = await Do(
                () => Result.Fail<IEnumerable<int>>("prev-err"),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => called = true);
        #endregion

        #region Assert
        called.ShouldBeFalse();
        rop.Result.Errors[0].Message.ShouldBe("prev-err");
        #endregion
    }

    [Fact(DisplayName = "IfNoData — handler возвращает null, сохраняется входная коллекция")]
    public async Task IfNoData_when_handler_returns_null_preserves_input()
    {
        #region Arrange
        int[] input = Array.Empty<int>();
        #endregion

        #region Act
        RopResult<int[]> rop = await Do(
                () => input,
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData(_ => (int[])null);
        #endregion

        #region Assert
        rop.Result.IsSuccess.ShouldBeTrue();
        rop.Result.Value.ShouldBeSameAs(input);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — null handler кидает ArgumentNullException")]
    public async Task IfNoData_when_handler_is_null_throws_argument_null_exception()
    {
        #region Arrange
        #endregion

        #region Act
        Task<RopResult<IEnumerable<int>>> action() => Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData((Action<NoDataContext>)null)
            .AsTask();
        #endregion

        #region Assert
        await Should.ThrowAsync<ArgumentNullException>(action);
        #endregion
    }

    [Fact(DisplayName = "IfNoData — null async handler кидает ArgumentNullException")]
    public async Task IfNoData_when_async_handler_is_null_throws_argument_null_exception()
    {
        #region Arrange
        #endregion

        #region Act
        Task<RopResult<IEnumerable<int>>> action() => Do(
                () => (IEnumerable<int>)Array.Empty<int>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .IfNoData((Func<NoDataContext, Task<IEnumerable<int>>>)null)
            .AsTask();
        #endregion

        #region Assert
        await Should.ThrowAsync<ArgumentNullException>(action);
        #endregion
    }

    [Fact(DisplayName = "IsPipelineTerminated — false при обычной ошибке")]
    public async Task IsPipelineTerminated_when_not_terminated_returns_false()
    {
        #region Arrange
        #endregion

        #region Act
        RopResult<int> rop = await Do(() => Result.Fail<int>("err"), cancellationToken: TestContext.Current.CancellationToken);
        #endregion

        #region Assert
        rop.Result.IsPipelineTerminated().ShouldBeFalse();
        #endregion
    }

    #endregion

    private sealed record Item(int GroupId, string Name);

    private sealed class TestGrouping<TKey, TElement>(TKey key, IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;

        public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class NonGenericNumbers(params int[] items) : IEnumerable<int>
    {
        private readonly int[] _items = items;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
