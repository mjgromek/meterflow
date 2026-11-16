using System.ComponentModel;
using System.Net;
using ModelContextProtocol.Server;

namespace MeterFlow.Mcp;

/// <summary>Typed client for the MeterFlow REST API; returns the JSON body, or an error text when the call fails.</summary>
public sealed class MeterFlowApi(HttpClient http)
{
    public async Task<string> GetAsync(string pathAndQuery, CancellationToken cancel)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(pathAndQuery, cancel);
        }
        catch (HttpRequestException e)
        {
            return $"Error: the MeterFlow API at {http.BaseAddress} is unreachable: {e.Message}";
        }
        catch (TaskCanceledException) when (!cancel.IsCancellationRequested)
        {
            return $"Error: the MeterFlow API at {http.BaseAddress} did not respond in time ({http.Timeout.TotalSeconds:0.##} s).";
        }
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return "Error: the API rejected the request. Times must be ISO 8601 with an offset or Z "
                    + "(e.g. 2026-10-25T00:00:00+02:00), from must be before to, and an EAN has 18 digits with a valid check digit.";
            }
            if (!response.IsSuccessStatusCode)
            {
                return $"Error: the MeterFlow API at {http.BaseAddress} answered {(int)response.StatusCode} {response.ReasonPhrase}.";
            }
            return await response.Content.ReadAsStringAsync(cancel);
        }
    }
}

[McpServerToolType]
public static class MeterFlowTools
{
    private static string Q(string text) => Uri.EscapeDataString(text);

    [McpServerTool(Name = "list_meters", ReadOnly = true)]
    [Description("Lists every meter that has stored readings, ordered by EAN, as JSON [{ean, readings, first, last}]. "
        + "readings is the number of 5-minute readings; first and last are UTC timestamps. An empty array means no data is stored yet.")]
    public static Task<string> ListMeters(MeterFlowApi api, CancellationToken cancel) =>
        api.GetAsync("/meters", cancel);

    [McpServerTool(Name = "get_consumption", ReadOnly = true)]
    [Description("Energy use of one meter per 15-minute quarter in [from, to), as JSON [{start, kwh}] ordered by start; kwh is kilowatt-hours. "
        + "An empty array means no readings in that range or an unknown meter.")]
    public static Task<string> GetConsumption(
        MeterFlowApi api,
        [Description("18-digit EAN code of the meter, as returned by list_meters.")] string ean,
        [Description("Inclusive start, ISO 8601 with an offset or Z, e.g. 2026-10-25T00:00:00+02:00.")] string from,
        [Description("Exclusive end, ISO 8601 with an offset or Z, after from.")] string to,
        CancellationToken cancel) =>
        api.GetAsync($"/meters/{Q(ean)}/consumption?from={Q(from)}&to={Q(to)}", cancel);

    [McpServerTool(Name = "find_gaps", ReadOnly = true)]
    [Description("Missing 5-minute readings per meter in [from, to), as JSON [{ean, count, moments}] ordered by count, most gaps first; "
        + "moments are the missing UTC timestamps. Only meters with readings in the range appear; every missing moment in the range counts, "
        + "including before a meter's first and after its last reading. An empty array means no meter has readings in that range.")]
    public static Task<string> FindGaps(
        MeterFlowApi api,
        [Description("Inclusive start, ISO 8601 with an offset or Z, e.g. 2026-10-25T00:00:00+02:00.")] string from,
        [Description("Exclusive end, ISO 8601 with an offset or Z, after from.")] string to,
        CancellationToken cancel) =>
        api.GetAsync($"/gaps?from={Q(from)}&to={Q(to)}", cancel);
}
