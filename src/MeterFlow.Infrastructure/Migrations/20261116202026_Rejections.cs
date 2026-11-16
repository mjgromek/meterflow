using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeterFlow.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261116202026_Rejections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Rejections",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Ean = table.Column<string>(type: "TEXT", maxLength: 18, nullable: false),
                MomentUtcTicks = table.Column<long>(type: "INTEGER", nullable: true),
                Reason = table.Column<string>(type: "TEXT", nullable: false),
                ReceivedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rejections", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Rejections");
    }
}
