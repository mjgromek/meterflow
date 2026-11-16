using MeterFlow.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MeterFlow.Infrastructure;

public class SqliteReadingStore(MeterFlowDbContext db) : IReadingStore
{
    public bool Add(Reading reading)
    {
        db.Readings.Add(reading);
        try
        {
            db.SaveChanges();
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return false;
        }
        finally
        {
            // Untracked so the same key can be added again in this context without a tracking conflict.
            db.ChangeTracker.Clear();
        }
    }

    public IEnumerable<Reading> ForMeter(Ean ean, DateTimeOffset from, DateTimeOffset to) =>
        db.Readings.AsNoTracking()
            .Where(r => r.Ean == ean && r.Moment >= from && r.Moment < to)
            .OrderBy(r => r.Moment)
            .ToList();

    public IEnumerable<Reading> Between(DateTimeOffset from, DateTimeOffset to) =>
        db.Readings.AsNoTracking()
            .Where(r => r.Moment >= from && r.Moment < to)
            .ToList();
}
