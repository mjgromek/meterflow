using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MeterFlow.Domain;
using MeterFlow.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MeterFlowDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("MeterFlow")));
builder.Services.AddScoped<IReadingStore, SqliteReadingStore>();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"];
if (frontendOrigin is not null)
{
    builder.Services.AddCors(o => o.AddPolicy("frontend", p => p.WithOrigins(frontendOrigin).WithMethods("GET")));
}
var queue = Channel.CreateBounded<Reading>(new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
builder.Services.AddSingleton(queue.Reader);
builder.Services.AddSingleton(queue.Writer);
builder.Services.AddHostedService<ReadingProcessor>();
var app = builder.Build();

// The build-time OpenAPI generator runs this entry point; it must not create or migrate a database.
if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<MeterFlowDbContext>().Database.Migrate();
}

if (frontendOrigin is not null)
{
    app.UseCors("frontend");
}
app.MapOpenApi();

var amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

app.MapGet("/health", () => TypedResults.Ok());

// An explicit offset or Z is required; "K" would also match no offset at all.
string[] momentFormats = ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];
bool TryParseMoment(string? text, out DateTimeOffset moment) =>
    DateTimeOffset.TryParseExact(text, momentFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out moment);

bool TryParseDay(string text, out DateOnly day) =>
    DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

app.MapGet("/meters/{ean}/consumption", Results<Ok<IEnumerable<Quarter>>, BadRequest> (string ean, string? from, string? to, IReadingStore store) =>
    Ean.TryParse(ean, out var meter) && TryParseMoment(from, out var start) && TryParseMoment(to, out var end) && start < end
        ? TypedResults.Ok(QuarterHours.Aggregate(store.ForMeter(meter, start, end))
            .OrderBy(q => q.Start)
            .Select(q => new Quarter(q.Start, q.Kwh)))
        : TypedResults.BadRequest());

app.MapGet("/meters/{ean}/days/{date}", Results<Ok<MeterDay>, BadRequest> (string ean, string date, IReadingStore store) =>
{
    if (!Ean.TryParse(ean, out var meter) || !TryParseDay(date, out var day))
    {
        return TypedResults.BadRequest();
    }
    var starts = QuarterHours.ForLocalDay(day, amsterdam).ToList();
    var (from, to) = (starts[0], starts[^1] + QuarterHours.Length);
    var readings = store.ForMeter(meter, from, to).ToList();
    var byQuarter = readings.ToLookup(r => (int)((r.Moment - from) / QuarterHours.Length));
    var quarters = starts.Select((start, i) => new DayQuarter(
        start.ToUniversalTime(), byQuarter[i].Any() ? byQuarter[i].Sum(r => r.Kwh) : null, byQuarter[i].Count()));
    // shortcut: gaps only between the day's first and last reading, not at the day edges; see backlog.
    var gaps = ReadingChecks.FindGaps(readings).Select(g => g.Moment);
    var rejected = store.RejectionsBetween(from, to)
        .Where(r => r.Ean == meter)
        .OrderBy(r => r.Moment)
        .Select(r => new DayRejection(r.Moment, r.Reason));
    return TypedResults.Ok(new MeterDay([.. quarters], [.. gaps], [.. rejected]));
});

app.MapGet("/quality/{date}", Results<Ok<IEnumerable<MeterQuality>>, BadRequest> (string date, IReadingStore store) =>
{
    if (!TryParseDay(date, out var day))
    {
        return TypedResults.BadRequest();
    }
    var starts = QuarterHours.ForLocalDay(day, amsterdam).ToList();
    var (from, to) = (starts[0], starts[^1] + QuarterHours.Length);
    var readings = store.Between(from, to).ToList();
    var gaps = ReadingChecks.FindGaps(readings).CountBy(g => g.Ean).ToDictionary();
    var rejections = store.RejectionsBetween(from, to).ToList();
    return TypedResults.Ok<IEnumerable<MeterQuality>>(readings.Select(r => r.Ean).Concat(rejections.Select(r => r.Ean)).Distinct()
        .Select(ean => new MeterQuality(
            ean.Value,
            gaps.GetValueOrDefault(ean),
            rejections.Count(r => r.Ean == ean && r.Reason != RejectReason.Duplicate),
            rejections.Count(r => r.Ean == ean && r.Reason == RejectReason.Duplicate)))
        .OrderByDescending(q => q.Gaps + q.Rejected + q.Duplicates)
        .ThenBy(q => q.Ean, StringComparer.Ordinal));
});

app.MapGet("/meters", (IReadingStore store) =>
    store.Meters().Select(m => new MeterRow(m.Ean.Value, m.Readings, m.First, m.Last)));

app.MapGet("/gaps", Results<Ok<IEnumerable<MeterGaps>>, BadRequest> (string? from, string? to, IReadingStore store) =>
    TryParseMoment(from, out var start) && TryParseMoment(to, out var end) && start < end
        ? TypedResults.Ok<IEnumerable<MeterGaps>>(store.Between(start, end)
            .GroupBy(r => r.Ean)
            .Select(meter =>
            {
                var moments = ReadingChecks.FindGaps(meter).Select(g => g.Moment).ToList();
                return new MeterGaps(meter.Key.Value, moments.Count, moments);
            })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Ean, StringComparer.Ordinal))
        : TypedResults.BadRequest());

app.MapPost("/exports/{date}", async Task<Results<Ok<ExportResult>, BadRequest>> (string date, IReadingStore store, IConfiguration config, CancellationToken cancel) =>
{
    if (!TryParseDay(date, out var day))
    {
        return TypedResults.BadRequest();
    }
    var (path, rows) = await QuarterExport.WriteAsync(store, day, config["Export:Root"] ?? "exports", cancel);
    return TypedResults.Ok(new ExportResult(path, rows));
});

app.MapPost("/readings", async (ReadingItem?[] items, ChannelWriter<Reading> queue, IReadingStore store, ILogger<Program> logger, CancellationToken cancel) =>
{
    int accepted = 0;
    List<RejectedItem> rejected = [];
    List<Rejection> attributable = [];
    for (int i = 0; i < items.Length; i++)
    {
        RejectReason reason;
        if (items[i] is not { Ean: { } text, Moment: { } momentText, Kwh: { } kwh } || !TryParseMoment(momentText, out var moment))
        {
            reason = RejectReason.BadFormat;
        }
        else if (!Ean.TryParse(text, out var ean))
        {
            reason = RejectReason.InvalidEan;
        }
        else if (kwh < 0)
        {
            reason = RejectReason.NegativeKwh;
        }
        else
        {
            // Waits while the queue is full; duplicates are detected later, when stored.
            await queue.WriteAsync(new Reading(ean, moment, kwh), cancel);
            accepted++;
            continue;
        }
        rejected.Add(new RejectedItem(i, reason.ToString()));
        if (Ean.TryParse(items[i]?.Ean, out var known))
        {
            attributable.Add(new Rejection(known, TryParseMoment(items[i]?.Moment, out var parsed) ? parsed : null, reason, DateTimeOffset.UtcNow));
        }
    }
    if (attributable.Count > 0)
    {
        store.AddRejections(attributable);
    }
    logger.LogInformation("POST /readings: {Accepted} accepted, {Rejected} rejected", accepted, rejected.Count);
    return TypedResults.Accepted((string?)null, new PostReadingsResult(accepted, rejected));
});

app.Run();

// Nullable so a missing field is rejected per item instead of stored as a default.
// Moment is a string so a bad or offset-less moment is rejected per item, not as a 400.
record ReadingItem(string? Ean, string? Moment, decimal? Kwh);

record RejectedItem(int Index, string Reason);

record PostReadingsResult(int Accepted, IReadOnlyList<RejectedItem> Rejected);

record Quarter(DateTimeOffset Start, decimal Kwh);

record ExportResult(string Path, int Rows);

record MeterRow(string Ean, int Readings, DateTimeOffset First, DateTimeOffset Last);

record MeterGaps(string Ean, int Count, IReadOnlyList<DateTimeOffset> Moments);

record DayQuarter(DateTimeOffset Start, decimal? Kwh, int Readings);

record DayRejection(DateTimeOffset? Moment, RejectReason Reason);

record MeterDay(IReadOnlyList<DayQuarter> Quarters, IReadOnlyList<DateTimeOffset> Gaps, IReadOnlyList<DayRejection> Rejected);

record MeterQuality(string Ean, int Gaps, int Rejected, int Duplicates);
