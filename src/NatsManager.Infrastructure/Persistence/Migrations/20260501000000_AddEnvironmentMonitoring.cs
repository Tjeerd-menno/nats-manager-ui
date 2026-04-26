using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NatsManager.Infrastructure.Persistence.Migrations
{
    public partial class AddEnvironmentMonitoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MonitoringUrl",
                table: "Environments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonitoringPollingIntervalSeconds",
                table: "Environments",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonitoringUrl",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "MonitoringPollingIntervalSeconds",
                table: "Environments");
        }
    }
}
