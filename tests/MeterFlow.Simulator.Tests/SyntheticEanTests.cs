namespace MeterFlow.Simulator.Tests;

public class SyntheticEanTests
{
    [Fact]
    public void Each_index_gives_a_distinct_valid_ean()
    {
        var eans = Enumerable.Range(0, 1000).Select(SyntheticEan.For).ToList();

        Assert.Equal(1000, eans.Distinct().Count());
        Assert.All(eans, e => Assert.StartsWith("999", e.Value));
    }
}
