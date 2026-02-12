using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelMartOrderProcessor.Migrations
{
    /// <inheritdoc />
    public partial class health_check_status_records : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "worker_health_status",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_check_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_processed = table.Column<int>(type: "integer", nullable: false),
                    total_errors = table.Column<int>(type: "integer", nullable: false),
                    error_rate = table.Column<double>(type: "double precision", nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_health_status", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_health_status_last_check_time",
                table: "worker_health_status",
                column: "last_check_time");

            migrationBuilder.CreateIndex(
                name: "IX_worker_health_status_worker_name",
                table: "worker_health_status",
                column: "worker_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_health_status");
        }
    }
}
