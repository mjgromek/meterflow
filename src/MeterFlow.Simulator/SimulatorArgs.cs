namespace MeterFlow.Simulator;

public record SimulatorOptions(int Meters, int Days, string Path, Uri? Api);

public static class SimulatorArgs
{
    public const string Usage = "Usage: MeterFlow.Simulator [meters=3] [days=1] [csv-path=readings.csv] [--api <http(s)://host:port>]";

    /// <summary>"--api &lt;url&gt;" may appear anywhere; the rest stay positional. Returns options, or an error to print with Usage.</summary>
    public static (SimulatorOptions? Options, string? Error) Parse(string[] args)
    {
        Uri? api = null;
        int apiAt = Array.IndexOf(args, "--api");
        if (apiAt >= 0)
        {
            if (apiAt + 1 >= args.Length
                || !Uri.TryCreate(args[apiAt + 1], UriKind.Absolute, out api)
                || (api.Scheme != Uri.UriSchemeHttp && api.Scheme != Uri.UriSchemeHttps))
            {
                return (null, "--api needs an http or https URL.");
            }
            args = [.. args[..apiAt], .. args[(apiAt + 2)..]];
        }

        int meters = 3, days = 1;
        if ((args.Length > 0 && (!int.TryParse(args[0], out meters) || meters < 1))
            || (args.Length > 1 && (!int.TryParse(args[1], out days) || days < 1)))
        {
            return (null, "meters and days must be positive whole numbers.");
        }
        return (new SimulatorOptions(meters, days, args.Length > 2 ? args[2] : "readings.csv", api), null);
    }
}
