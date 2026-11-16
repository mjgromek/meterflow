using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeterFlow.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261116192955_MomentIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Readings_MomentUtcTicks",
            table: "Readings",
            column: "MomentUtcTicks");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Readings_MomentUtcTicks",
            table: "Readings");
    }
}
