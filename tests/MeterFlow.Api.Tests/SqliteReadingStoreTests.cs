using MeterFlow.Domain;
using MeterFlow.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MeterFlow.Api.Tests;

public sealed class SqliteReadingStoreTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"meterflow-{Guid.NewGuid():N}.db");
    private readonly MeterFlowDbContext db;
    private readonly SqliteReadingStore store;
    private readonly Ean a = Ean.Parse(ReadingsTests.TestEan(30));
    private readonly Ean b = Ean.Parse(ReadingsTests.TestEan(31));
    private readonly DateTimeOffset t = new(2026, 3, 2, 10, 0, 0, TimeSpan.FromHours(1));

    public SqliteReadingStoreTests()
    {
        db = new MeterFlowDbContext(new DbContextOptionsBuilder<MeterFlowDbContext>().UseSqlite($"Data Source={path}").Options);
        db.Database.Migrate();
        store = new SqliteReadingStore(db);
    }

    public void Dispose()
    {
        db.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(path + suffix);
        }
    }

    [Fact]
    public void Add_stores_a_reading_read_back_as_the_same_instant_in_utc()
    {
        Assert.True(store.Add(new Reading(a, t, 0.25m)));

        var stored = Assert.Single(store.ForMeter(a, DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
        Assert.Equal(new Reading(a, t, 0.25m), stored);
        Assert.Equal(TimeSpan.Zero, stored.Moment.Offset);
    }

    [Fact]
    public void Add_returns_false_for_the_same_instant_in_another_offset_and_the_store_stays_usable()
    {
        store.Add(new Reading(a, t, 0.25m));

        Assert.False(store.Add(new Reading(a, t.ToOffset(TimeSpan.Zero), 0.5m)));
        Assert.True(store.Add(new Reading(a, t.AddMinutes(5), 0.5m)));
        Assert.Equal([0.25m, 0.5m], store.ForMeter(a, DateTimeOffset.MinValue, DateTimeOffset.MaxValue).Select(r => r.Kwh));
    }

    [Fact]
    public void ForMeter_returns_one_meters_readings_in_from_inclusive_to_exclusive_order()
    {
        store.Add(new Reading(a, t.AddMinutes(10), 3m));
        store.Add(new Reading(a, t.AddMinutes(5), 2m));
        store.Add(new Reading(a, t, 1m));
        store.Add(new Reading(a, t.AddMinutes(-5), 0.5m));
        store.Add(new Reading(b, t.AddMinutes(5), 9m));

        var result = store.ForMeter(a, t, t.AddMinutes(10).ToOffset(TimeSpan.Zero));

        Assert.Equal([1m, 2m], result.Select(r => r.Kwh));
    }
}
