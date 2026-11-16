using System.Diagnostics.CodeAnalysis;

namespace MeterFlow.Simulator;

public record SimulatorOptions(int Meters, int Days, string Path, Uri? Api);

public static class SimulatorArgs
{
    public const string Usage = "Usage: MeterFlow.Simulator [meters=3] [days=1] [csv-path=readings.csv] [--api <http(s)://host:port>]; without --api, MeterFlow__ApiBaseUrl selects the API";

    /// <summary>
    /// "--api &lt;url&gt;" may appear anywhere; the rest stay positional. Without --api, a non-empty
    /// apiFromEnvironment (MeterFlow__ApiBaseUrl) selects HTTP mode. Returns options, or an error to print with Usage.
    /// </summary>
    public static (SimulatorOptions? Options, string? Error) Parse(string[] args, string? apiFromEnvironment = null)
    {
        Uri? api = null;
        int apiAt = Array.IndexOf(args, "--api");
        if (apiAt >= 0)
        {
            if (apiAt + 1 >= args.Length || !TryHttpUri(args[apiAt + 1], out api))
            {
                return (null, "--api needs an http or https URL.");
            }
            args = [.. args[..apiAt], .. args[(apiAt + 2)..]];
        }
        else if (!string.IsNullOrEmpty(apiFromEnvironment) && !TryHttpUri(apiFromEnvironment, out api))
        {
            return (null, "MeterFlow__ApiBaseUrl needs an http or https URL.");
        }

        int meters = 3, days = 1;
        if ((args.Length > 0 && (!int.TryParse(args[0], out meters) || meters < 1))
            || (args.Length > 1 && (!int.TryParse(args[1], out days) || days < 1)))
        {
            return (null, "meters and days must be positive whole numbers.");
        }
        return (new SimulatorOptions(meters, days, args.Length > 2 ? args[2] : "readings.csv", api), null);
    }

    private static bool TryHttpUri(string text, [NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(text, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
