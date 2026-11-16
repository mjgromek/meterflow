using MeterFlow.Simulator;

int meters = args.Length > 0 ? int.Parse(args[0]) : 3;
int days = args.Length > 1 ? int.Parse(args[1]) : 1;
string path = args.Length > 2 ? args[2] : "readings.csv";

// ponytail: UTC timestamps, so the profile peaks at 18:00 UTC; local-time shaping belongs to M3.
var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days), TimeSpan.Zero);
var rng = new Random(42);

var readings = Enumerable.Range(0, meters).SelectMany(i =>
    Faults.Inject(MeterSeries.For(SyntheticEan.For(i), start, days * 288, rng), rng, 0.01, 0.005));

using var writer = File.CreateText(path);
CsvExport.Write(writer, readings);
Console.WriteLine($"Wrote {meters} meters x {days} days to {path}");
