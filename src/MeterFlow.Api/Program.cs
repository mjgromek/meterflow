using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MeterFlow.Domain;
using MeterFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MeterFlowDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("MeterFlow")));
builder.Services.AddScoped<IReadingStore, SqliteReadingStore>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
var queue = Channel.CreateBounded<Reading>(new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
builder.Services.AddSingleton(queue.Reader);
builder.Services.AddSingleton(queue.Writer);
builder.Services.AddHostedService<ReadingProcessor>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MeterFlowDbContext>().Database.Migrate();
}

app.MapGet("/health", () => Results.Ok());

// An explicit offset or Z is required; "K" would also match no offset at all.
string[] momentFormats = ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];
bool TryParseMoment(string? text, out DateTimeOffset moment) =>
    DateTimeOffset.TryParseExact(text, momentFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out moment);

app.MapGet("/meters/{ean}/consumption", (string ean, string? from, string? to, IReadingStore store) =>
    Ean.TryParse(ean, out var meter) && TryParseMoment(from, out var start) && TryParseMoment(to, out var end) && start < end
        ? Results.Ok(QuarterHours.Aggregate(store.ForMeter(meter, start, end))
            .OrderBy(q => q.Start)
            .Select(q => new Quarter(q.Start, q.Kwh)))
        : Results.BadRequest());

app.MapPost("/readings", async (ReadingItem?[] items, ChannelWriter<Reading> queue, ILogger<Program> logger, CancellationToken cancel) =>
{
    int accepted = 0;
    List<Rejection> rejected = [];
    for (int i = 0; i < items.Length; i++)
    {
        if (items[i] is not { Ean: { } text, Moment: { } momentText, Kwh: { } kwh } || !TryParseMoment(momentText, out var moment))
        {
            rejected.Add(new Rejection(i, nameof(RejectReason.BadFormat)));
        }
        else if (!Ean.TryParse(text, out var ean))
        {
            rejected.Add(new Rejection(i, nameof(RejectReason.InvalidEan)));
        }
        else if (kwh < 0)
        {
            rejected.Add(new Rejection(i, nameof(RejectReason.NegativeKwh)));
        }
        else
        {
            // Waits while the queue is full; duplicates are detected later, when stored.
            await queue.WriteAsync(new Reading(ean, moment, kwh), cancel);
            accepted++;
        }
    }
    logger.LogInformation("POST /readings: {Accepted} accepted, {Rejected} rejected", accepted, rejected.Count);
    return Results.Accepted(value: new PostReadingsResult(accepted, rejected));
});

app.Run();

// Nullable so a missing field is rejected per item instead of stored as a default.
// Moment is a string so a bad or offset-less moment is rejected per item, not as a 400.
record ReadingItem(string? Ean, string? Moment, decimal? Kwh);

record Rejection(int Index, string Reason);

record PostReadingsResult(int Accepted, IReadOnlyList<Rejection> Rejected);

record Quarter(DateTimeOffset Start, decimal Kwh);
