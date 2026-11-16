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

    [Fact]
    public void Environment_api_url_selects_http_mode_unless_api_is_given()
    {
        var (fromEnv, _) = SimulatorArgs.Parse(["2"], "http://env:5265");
        var (fromArgs, _) = SimulatorArgs.Parse(["--api", "http://args:5265"], "http://env:5265");
        var (csv, _) = SimulatorArgs.Parse([], "");
        var (_, badEnv) = SimulatorArgs.Parse([], "not a url");

        Assert.Equal(new Uri("http://env:5265"), fromEnv?.Api);
        Assert.Equal(new Uri("http://args:5265"), fromArgs?.Api);
        Assert.Null(csv?.Api);
        Assert.NotNull(csv);
        Assert.False(string.IsNullOrWhiteSpace(badEnv));
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
