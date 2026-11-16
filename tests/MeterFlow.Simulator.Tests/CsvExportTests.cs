using System.Globalization;
using MeterFlow.Domain;

namespace MeterFlow.Simulator.Tests;

public class CsvExportTests
{
    private static readonly Ean Meter = Ean.Parse("123456789012345675");
    private static readonly DateTimeOffset Moment = new(2026, 11, 16, 18, 5, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Writes_header_and_one_line_per_reading_regardless_of_culture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
        try
        {
            var writer = new StringWriter { NewLine = "\n" };

            CsvExport.Write(writer, [new Reading(Meter, Moment, 0.247m), new Reading(Meter, Moment.AddMinutes(5), 1.5m)]);

            Assert.Equal(
                "ean,moment,kwh\n" +
                "123456789012345675,2026-11-16T18:05:00.0000000+01:00,0.247\n" +
                "123456789012345675,2026-11-16T18:10:00.0000000+01:00,1.5\n",
                writer.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
