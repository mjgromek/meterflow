namespace MeterFlow.Domain;

public interface IReadingStore
{
    /// <summary>Stores the reading; false when the same (Ean, instant) is already stored.</summary>
    bool Add(Reading reading);

    /// <summary>Readings for one meter with from &lt;= Moment &lt; to, ordered by Moment.</summary>
    IEnumerable<Reading> ForMeter(Ean ean, DateTimeOffset from, DateTimeOffset to);
}
