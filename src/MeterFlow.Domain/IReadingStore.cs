namespace MeterFlow.Domain;

public interface IReadingStore
{
    /// <summary>Stores the reading; false when the same (Ean, instant) is already stored.</summary>
    bool Add(Reading reading);

    /// <summary>Readings for one meter with from &lt;= Moment &lt; to, ordered by Moment.</summary>
    IEnumerable<Reading> ForMeter(Ean ean, DateTimeOffset from, DateTimeOffset to);

    /// <summary>Readings for all meters with from &lt;= Moment &lt; to, unordered.</summary>
    IEnumerable<Reading> Between(DateTimeOffset from, DateTimeOffset to);

    /// <summary>One summary per stored meter, ordered by Ean.</summary>
    IEnumerable<MeterSummary> Meters();

    void AddRejections(IEnumerable<Rejection> rejections);

    /// <summary>Rejections with from &lt;= Moment &lt; to, or ReceivedAt in that range when Moment is unknown.</summary>
    IEnumerable<Rejection> RejectionsBetween(DateTimeOffset from, DateTimeOffset to);
}

public record MeterSummary(Ean Ean, int Readings, DateTimeOffset First, DateTimeOffset Last);

/// <summary>A rejected reading that names a valid meter; Moment is null when it could not be parsed.</summary>
public record Rejection(Ean Ean, DateTimeOffset? Moment, RejectReason Reason, DateTimeOffset ReceivedAt);
