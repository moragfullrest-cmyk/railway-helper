using FluentResults;
using RailwayHelper;
using static RailwayHelper.RailwayHelper;

Console.WriteLine("=== RailwayHelper Samples ===\n");

await RunHappyPathSample();
await RunLabeledFailureSample();
await RunIfNoDataSample();
await RunDoEachSample();

static async Task RunHappyPathSample()
{
    Console.WriteLine("--- Happy path: Do -> Peek -> Next ---");

    List<string> trace = [];

    Result<int> result = await Do(() => new[] { 10, 20, 30 })
        .Peek(xs => trace.Add($"loaded {xs.Length} items"))
        .Next(xs => xs.Sum())
        .Peek((int sum) => trace.Add($"sum = {sum}"))
        .Next(sum => sum * 2);

    Console.WriteLine($"Success: {result.IsSuccess}, Value: {result.Value}");
    Console.WriteLine($"Trace: {string.Join(" -> ", trace)}\n");
}

static async Task RunLabeledFailureSample()
{
    Console.WriteLine("--- Labeled failure + OnFailure ---");

    const string ValidateLabel = "validate";
    int capturedId = 0;

    Result<int> result = await Do(42, id => id > 0 ? Result.Ok(id) : Result.Fail<int>("bad id"), label: ValidateLabel)
        .Next(id => Result.Fail<int>("downstream error"), label: "process")
        .OnFailure(failed =>
        {
            failed.WhenCallData<int>("process", (id, _) => capturedId = id);
        });

    Console.WriteLine($"Handled failure: {result.Errors[0].GetType().Name}");
    Console.WriteLine($"Captured id from process step: {capturedId}\n");
}

static async Task RunIfNoDataSample()
{
    Console.WriteLine("--- IfNoData: substitute defaults ---");

    Result<IEnumerable<int>> result = await Do(() => (IEnumerable<int>)Array.Empty<int>())
        .IfNoData(_ => Result.Ok( new[] { 1, 2, 3 }))
        .Next(xs => xs.Select(x => x * 100));

    Console.WriteLine($"Values: [{string.Join(", ", result.Value)}]\n");
}

static async Task RunDoEachSample()
{
    Console.WriteLine("--- DoEach: sequential processing ---");

    Result<IEnumerable<string>> result = await DoEach(
        new[] { "alpha", "beta", "gamma" },
        word => word.ToUpperInvariant());

    Console.WriteLine($"Processed: [{string.Join(", ", result.Value)}]\n");
}
