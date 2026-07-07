using BenchmarkDotNet.Attributes;
using FluentResults;
using static RailwayHelper.RailwayHelper;

namespace RailwayHelper.Benchmarks;

[MemoryDiagnoser]
public class DoNextChainSmallBenchmarks
{
    private readonly int[] _smallInput = Enumerable.Range(1, 10).ToArray();

    [Benchmark(Baseline = true)]
    public Task<int> Baseline()
    {
        int result = _smallInput
            .Select(x => x + 1)
            .Sum() * 2;

        return Task.FromResult(result);
    }

    [Benchmark]
    public async Task<int> Railway()
    {
        RopResult<int> rop = await Do(() => _smallInput, cancellationToken: CancellationToken.None)
            .Next(items => items.Select(x => x + 1))
            .Next(items => items.Sum())
            .Next(sum => sum * 2);

        return rop.Result.Value;
    }
}

[MemoryDiagnoser]
public class NextEachTransformLargeBenchmarks
{
    private readonly int[] _largeInput = Enumerable.Range(1, 1_000).ToArray();

    [Benchmark(Baseline = true)]
    public Task<int[]> Baseline()
    {
        int[] result = _largeInput
            .Select(x => x + 1)
            .Select(x => x * 2)
            .ToArray();

        return Task.FromResult(result);
    }

    [Benchmark]
    public async Task<int[]> Railway()
    {
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)_largeInput, cancellationToken: CancellationToken.None)
            .NextEach(x => x + 1)
            .NextEach(x => x * 2);

        return rop.Result.Value.ToArray();
    }
}

[MemoryDiagnoser]
public class PeekEachWithSideEffectLargeBenchmarks
{
    private readonly int[] _largeInput = Enumerable.Range(1, 1_000).ToArray();

    [Benchmark(Baseline = true)]
    public Task<int[]> Baseline()
    {
        int touched = 0;
        foreach (int value in _largeInput)
            touched += value;

        _ = touched;
        return Task.FromResult(_largeInput.ToArray());
    }

    [Benchmark]
    public async Task<int[]> Railway()
    {
        int touched = 0;
        RopResult<IEnumerable<int>> rop = await Do(() => (IEnumerable<int>)_largeInput, cancellationToken: CancellationToken.None)
            .PeekEach(x => touched += x)
            .Next(items => items);

        _ = touched;
        return rop.Result.Value.ToArray();
    }
}

[MemoryDiagnoser]
public class FailureRouteWithOnFailureBenchmarks
{
    [Benchmark(Baseline = true)]
    public Task<Result<int>> Baseline()
    {
        Result<int> result = Result.Fail<int>("bench-fail");

        if (result.IsFailed)
        {
            _ = result.Errors;
            return Task.FromResult(Result.Fail<int>("bench-handled-failure"));
        }

        return Task.FromResult(result);
    }

    [Benchmark]
    public async Task<Result<int>> Railway()
    {
        return await Do(42, _ => Result.Fail<int>("bench-fail"), label: "bench", cancellationToken: CancellationToken.None)
            .OnFailure(_ => { });
    }
}
