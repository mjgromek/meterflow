using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace MeterFlow.Api.Tests;

/// <summary>The API on a throwaway SQLite file, deleted on dispose unless the file was passed in.</summary>
public class MeterFlowFactory : WebApplicationFactory<Program>
{
    private readonly bool ownsFile;

    public MeterFlowFactory() : this(Path.Combine(Path.GetTempPath(), $"meterflow-{Guid.NewGuid():N}.db")) => ownsFile = true;

    internal MeterFlowFactory(string path) => DbPath = path;

    public string DbPath { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:MeterFlow", $"Data Source={DbPath}");

    public static void DeleteDatabase(string path)
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(path + suffix);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (ownsFile)
        {
            DeleteDatabase(DbPath);
        }
    }
}
