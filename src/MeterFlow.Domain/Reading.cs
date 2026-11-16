namespace MeterFlow.Domain;

public record Reading(Ean Ean, DateTimeOffset Moment, decimal Kwh);
