using System.Globalization;
using MeterFlow.Domain;

namespace MeterFlow.Simulator;

public static class CsvExport
{
    public static void Write(TextWriter writer, IEnumerable<Reading> readings)
    {
        writer.WriteLine("ean,moment,kwh");
        foreach (var r in readings)
        {
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{r.Ean},{r.Moment:O},{r.Kwh}"));
        }
    }
}
