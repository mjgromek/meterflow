using MeterFlow.Domain;

namespace MeterFlow.Domain.Tests;

public class ReadingCsvTests
{
    private const string Header = "ean,moment,kwh";
    private const string GoodRow = "123456789012345675,2026-10-25T02:30:00.0000000+02:00,1.25";

    [Fact]
    public void A_written_row_parses_to_a_reading_with_its_offset_kept()
    {
        var result = ReadingCsv.Parse([Header, GoodRow]);

        Reading expected = new(
            Ean.Parse("123456789012345675"),
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)),
            1.25m);
        Assert.Equal([expected], result.Readings);
        Assert.Equal(TimeSpan.FromHours(2), result.Readings[0].Moment.Offset);
        Assert.Empty(result.Rejected);
    }

    [Theory]
    [InlineData("123456789012345675,2026-10-25T02:30:00.0000000+02:00", RejectReason.BadFormat)]
    [InlineData("123456789012345675,2026-10-25T02:30:00.0000000+02:00,1.25,x", RejectReason.BadFormat)]
    [InlineData("123456789012345675,not-a-moment,1.25", RejectReason.BadFormat)]
    [InlineData("123456789012345675,2026-10-25T02:30:00.0000000+02:00,1,25", RejectReason.BadFormat)]
    [InlineData("123456789012345675,2026-10-25T02:30:00.0000000+02:00,abc", RejectReason.BadFormat)]
    [InlineData("123456789012345670,2026-10-25T02:30:00.0000000+02:00,1.25", RejectReason.InvalidEan)]
    [InlineData("123456789012345675,2026-10-25T02:30:00.0000000+02:00,-0.5", RejectReason.NegativeKwh)]
    public void A_bad_row_is_rejected_with_its_line_number_and_reason(string row, RejectReason reason)
    {
        var result = ReadingCsv.Parse([Header, row]);

        Assert.Empty(result.Readings);
        Assert.Equal([new RejectedRow(2, reason)], result.Rejected);
    }

    [Fact]
    public void Zero_kwh_is_a_valid_reading()
    {
        var result = ReadingCsv.Parse([Header, "123456789012345675,2026-10-25T02:30:00.0000000+02:00,0"]);

        Assert.Equal(0m, Assert.Single(result.Readings).Kwh);
        Assert.Empty(result.Rejected);
    }

    [Fact]
    public void Windows_line_endings_are_ignored()
    {
        var result = ReadingCsv.Parse([Header + "\r", GoodRow + "\r", "garbage\r"]);

        Assert.Single(result.Readings);
        Assert.Equal([new RejectedRow(3, RejectReason.BadFormat)], result.Rejected);
    }

    [Fact]
    public void A_file_without_a_header_keeps_its_first_row()
    {
        var result = ReadingCsv.Parse([GoodRow, "garbage"]);

        Assert.Single(result.Readings);
        Assert.Equal([new RejectedRow(2, RejectReason.BadFormat)], result.Rejected);
    }

    [Fact]
    public void Good_rows_after_a_bad_row_are_still_returned()
    {
        var result = ReadingCsv.Parse([Header, "garbage", GoodRow]);

        Assert.Single(result.Readings);
        Assert.Equal([new RejectedRow(2, RejectReason.BadFormat)], result.Rejected);
    }
}
