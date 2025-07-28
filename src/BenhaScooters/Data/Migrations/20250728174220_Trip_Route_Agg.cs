using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trip_Route_Agg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripFares");

            migrationBuilder.DropIndex(
                name: "IX_Trips_CreatedAt",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DriverArrivedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Trips");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedFare_Amount",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFare",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TripFare_BaseFare",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TripFare_DistanceFare",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TripFare_DurationFare",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TripFare_SurgeMultiplier",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripEvent",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripEvent", x => new { x.TripId, x.Id });
                    table.ForeignKey(
                        name: "FK_TripEvent_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripEvent");

            migrationBuilder.DropIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes");

            migrationBuilder.DropColumn(
                name: "TotalFare",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripFare_BaseFare",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripFare_DistanceFare",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripFare_DurationFare",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripFare_SurgeMultiplier",
                table: "Trips");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedFare_Amount",
                table: "Trips",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DriverArrivedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripFares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaseFare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DistanceFare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationFare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SurgeMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripFares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripFares_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CreatedAt",
                table: "Trips",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes",
                column: "TripId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_TripId",
                table: "TripFares",
                column: "TripId",
                unique: true);
        }
    }
}
