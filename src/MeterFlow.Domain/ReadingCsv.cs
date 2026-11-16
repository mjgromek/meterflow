using System.Globalization;

namespace MeterFlow.Domain;

public enum RejectReason { BadFormat, InvalidEan, NegativeKwh, Duplicate }

public record RejectedRow(int LineNumber, RejectReason Reason);

public record ReadingCsvResult(IReadOnlyList<Reading> Readings, IReadOnlyList<RejectedRow> Rejected);

/// <summary>Reads the "ean,moment,kwh" format CsvExport writes. Bad rows are reported, never thrown.</summary>
public static class ReadingCsv
{
    private const string Header = "ean,moment,kwh";

    public static ReadingCsvResult Parse(IEnumerable<string> lines)
    {
        List<Reading> readings = [];
        List<RejectedRow> rejected = [];
        int lineNumber = 0;
        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.TrimEnd('\r');
            if (lineNumber == 1 && line == Header)
            {
                continue;
            }

            if (ParseRow(line, out var reason) is { } reading)
            {
                readings.Add(reading);
            }
            else
            {
                rejected.Add(new RejectedRow(lineNumber, reason));
            }
        }
        return new ReadingCsvResult(readings, rejected);
    }

    private static Reading? ParseRow(string line, out RejectReason reason)
    {
        var columns = line.Split(',');
        if (columns.Length != 3
            || !DateTimeOffset.TryParseExact(columns[1], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment)
            || !decimal.TryParse(columns[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var kwh))
        {
            reason = RejectReason.BadFormat;
            return null;
        }
        if (!Ean.TryParse(columns[0], out var ean))
        {
            reason = RejectReason.InvalidEan;
            return null;
        }
        if (kwh < 0)
        {
            reason = RejectReason.NegativeKwh;
            return null;
        }
        reason = default;
        return new Reading(ean, moment, kwh);
    }
}
