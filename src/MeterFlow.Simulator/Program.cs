using MeterFlow.Simulator;

var (options, error) = SimulatorArgs.Parse(args, Environment.GetEnvironmentVariable("MeterFlow__ApiBaseUrl"));
if (options is null)
{
    Console.Error.WriteLine($"Error: {error}");
    Console.Error.WriteLine(SimulatorArgs.Usage);
    return 1;
}
var (meters, days, path, api) = options;

// UTC moments, so the profile peaks at 18:00 UTC.
var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days), TimeSpan.Zero);
var rng = new Random(42);

var readings = Enumerable.Range(0, meters).SelectMany(i =>
    Faults.Inject(MeterSeries.For(SyntheticEan.For(i), start, days * 288, rng), rng, 0.01, 0.005));

if (api is not null)
{
    using var client = new HttpClient { BaseAddress = api };
    try
    {
        var (accepted, rejected) = await ApiSender.SendAsync(client, readings, batchSize: 500);
        Console.WriteLine($"Sent {meters} meters x {days} days to {api}: {accepted} accepted, {rejected} rejected");
        return 0;
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
    {
        Console.Error.WriteLine($"Error: sending readings to {api} failed: {e.Message}");
        return 1;
    }
}

try
{
    using var writer = File.CreateText(path);
    CsvExport.Write(writer, readings);
}
catch (Exception e) when (e is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: writing {path} failed: {e.Message}");
    return 1;
}
Console.WriteLine($"Wrote {meters} meters x {days} days to {path}");
return 0;
