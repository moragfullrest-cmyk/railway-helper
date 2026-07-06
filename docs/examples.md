# Примеры

## 1. Простая цепочка

```csharp
using FluentResults;
using static RailwayHelper.RailwayHelper;

Result<int> result = await Do(() => new[] { 1, 2, 3 })
    .Next(xs => xs.Sum())
    .Next(sum => sum * 10);

// result.Value == 60
```

## 2. Логирование через Peek

```csharp
List<int> log = [];

Result<int> result = await Do(() => 100)
    .Peek(x => log.Add(x))
    .Next(x => x / 2);

// log == [100], result.Value == 50
```

## 3. Обработка ошибок с метками

```csharp
const string Load = "load";
const string Send = "send";

Result result = await Do(orderId, id => LoadOrderAsync(id), label: Load)
    .Next(order => SendToGatewayAsync(order), label: Send)
    .OnFailure(failed =>
    {
        failed.WhenCallData<int>(Load, (id, _) => Console.WriteLine($"Load failed for {id}"));
        failed.WhenCallData<Order>(Send, (order, _) => Console.WriteLine($"Send failed for {order.Id}"));
    });
```

## 4. Пакетная обработка с прерыванием

```csharp
Result<IEnumerable<int>> result = await DoEach(
    new[] { 1, 2, 3 },
    x => x < 3 ? Result.Ok(x * 2) : Result.Fail<int>("limit"));

// SequenceAbortedError<int> с AbortedOn == 3
```

## 5. Пустая выборка — мягкое завершение

```csharp
Result<IEnumerable<Document>> result = await Do(() => QueryDocumentsAsync())
    .IfNoData(ctx => ctx.Terminate())
    .Next(docs => ProcessAllAsync(docs));

if (result.IsPipelineTerminated())
    Console.WriteLine("Nothing to process");
```

## 6. Пустая выборка — подстановка значений по умолчанию

```csharp
Result<IEnumerable<int>> result = await Do(() => Array.Empty<int>())
    .IfNoData(_ => new[] { 1, 2, 3 })
    .Next(xs => xs.Select(x => x * 10));

// [10, 20, 30]
```

## 7. Вложенные DoEach

```csharp
int[] orgs = [1, 2];
var docsByOrg = new Dictionary<int, int[]>
{
    [1] = [10, 11],
    [2] = [20]
};

await DoEach(orgs, org => org)
    .NextEach(async org =>
    {
        await DoEach(docsByOrg[org], doc => ProcessDocAsync(doc));
    });
```

## 8. Группировка и итерация

```csharp
var items = new[]
{
    new { GroupId = 1, Name = "a" },
    new { GroupId = 1, Name = "b" },
    new { GroupId = 2, Name = "c" }
};

Result<IEnumerable<int>> result = await Do(() => items)
    .Next(xs => xs.GroupBy(x => x.GroupId))
    .NextEach(g => g.Count());
```

## 9. CancellationToken

```csharp
CancellationToken ct = cancellationToken;

Result<int> result = await Do(
        () => FetchValueAsync(),
        cancellationToken: ct)
    .Next(v => ProcessAsync(v, ct));
```

## 10. Консольный пример

Полный исполняемый пример — проект `samples/RailwayHelper.Samples`:

```bash
dotnet run --project samples/RailwayHelper.Samples/RailwayHelper.Samples.csproj
```
