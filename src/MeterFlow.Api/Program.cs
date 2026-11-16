using System.Globalization;
using System.Text.Json.Serialization;
using MeterFlow.Api;
using MeterFlow.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IReadingStore, InMemoryReadingStore>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
var app = builder.Build();

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

app.MapPost("/readings", (ReadingItem?[] items, IReadingStore store, ILogger<Program> logger) =>
{
    int accepted = 0;
    List<Rejection> rejected = [];
    for (int i = 0; i < items.Length; i++)
    {
        var item = items[i];
        RejectReason? reason =
            item is not { Ean: { } text, Moment: { } momentText, Kwh: { } kwh }
            || !TryParseMoment(momentText, out var moment)
                ? RejectReason.BadFormat
            : !Ean.TryParse(text, out var ean) ? RejectReason.InvalidEan
            : kwh < 0 ? RejectReason.NegativeKwh
            : !store.Add(new Reading(ean, moment, kwh)) ? RejectReason.Duplicate
            : null;
        if (reason is null)
        {
            accepted++;
        }
        else
        {
            rejected.Add(new Rejection(i, reason.Value.ToString()));
        }
    }
    logger.LogInformation("POST /readings: {Accepted} accepted, {Rejected} rejected", accepted, rejected.Count);
    return new PostReadingsResult(accepted, rejected);
});

app.Run();

// Nullable so a missing field is rejected per item instead of stored as a default.
// Moment is a string so a bad or offset-less moment is rejected per item, not as a 400.
record ReadingItem(string? Ean, string? Moment, decimal? Kwh);

record Rejection(int Index, string Reason);

record PostReadingsResult(int Accepted, IReadOnlyList<Rejection> Rejected);

record Quarter(DateTimeOffset Start, decimal Kwh);
