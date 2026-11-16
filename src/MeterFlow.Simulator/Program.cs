using MeterFlow.Simulator;

// "--api <url>" may appear anywhere; the rest stay positional.
int apiAt = Array.IndexOf(args, "--api");
string? api = null;
if (apiAt >= 0)
{
    api = apiAt + 1 < args.Length ? args[apiAt + 1] : throw new ArgumentException("--api needs a URL.");
    args = [.. args[..apiAt], .. args[(apiAt + 2)..]];
}

int meters = args.Length > 0 ? int.Parse(args[0]) : 3;
int days = args.Length > 1 ? int.Parse(args[1]) : 1;
string path = args.Length > 2 ? args[2] : "readings.csv";

// ponytail: UTC timestamps, so the profile peaks at 18:00 UTC; local-time shaping belongs to M3.
var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days), TimeSpan.Zero);
var rng = new Random(42);

var readings = Enumerable.Range(0, meters).SelectMany(i =>
    Faults.Inject(MeterSeries.For(SyntheticEan.For(i), start, days * 288, rng), rng, 0.01, 0.005));

if (api is not null)
{
    using var client = new HttpClient { BaseAddress = new Uri(api) };
    var (accepted, rejected) = await ApiSender.SendAsync(client, readings, batchSize: 500);
    Console.WriteLine($"Sent {meters} meters x {days} days to {api}: {accepted} accepted, {rejected} rejected");
    return;
}

using var writer = File.CreateText(path);
CsvExport.Write(writer, readings);
Console.WriteLine($"Wrote {meters} meters x {days} days to {path}");
