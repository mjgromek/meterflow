using MeterFlow.Simulator;

namespace MeterFlow.Simulator.Tests;

public class SimulatorArgsTests
{
    [Fact]
    public void Api_may_appear_anywhere_and_the_rest_stay_positional()
    {
        var (options, error) = SimulatorArgs.Parse(["5", "--api", "http://localhost:5265", "2", "out.csv"]);

        Assert.Null(error);
        Assert.Equal(new SimulatorOptions(5, 2, "out.csv", new Uri("http://localhost:5265")), options);
    }

    [Theory]
    [InlineData("--api")]
    [InlineData("--api", "not a url")]
    [InlineData("--api", "ftp://localhost")]
    [InlineData("three")]
    public void Bad_arguments_give_an_error_and_no_options(params string[] args)
    {
        var (options, error) = SimulatorArgs.Parse(args);

        Assert.Null(options);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
