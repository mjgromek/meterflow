using MeterFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace MeterFlow.Infrastructure;

public class MeterFlowDbContext(DbContextOptions<MeterFlowDbContext> options) : DbContext(options)
{
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reading = modelBuilder.Entity<Reading>();
        // SQLite cannot compare or order DateTimeOffset, so the instant is stored as UTC ticks.
        reading.Property(r => r.Moment).HasColumnName("MomentUtcTicks")
            .HasConversion(m => m.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        reading.Property(r => r.Ean).HasConversion(e => e.Value, v => Ean.Parse(v)).HasMaxLength(Ean.Length);
        // The key is the unique index that rejects duplicates and serves ForMeter's range query.
        reading.HasKey(r => new { r.Ean, r.Moment });
        // Serves Between's all-meter range query, which the (Ean, Moment) key cannot.
        reading.HasIndex(r => r.Moment);
    }
}
