using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trip_Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ActualDistance",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ActualDuration",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "EstimatedDistance",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "EstimatedDuration",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FinalFare_ActualDistance",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FinalFare_ActualTime",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FinalFare_CalculatedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FinalFare_FinalAmount",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripRequestId",
                table: "Trips");

            migrationBuilder.AlterColumn<int>(
                name: "TripId",
                table: "LocationPings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TripFare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    BaseFare = table.Column<decimal>(type: "numeric", nullable: false),
                    DistanceFare = table.Column<decimal>(type: "numeric", nullable: false),
                    TimeFare = table.Column<decimal>(type: "numeric", nullable: false),
                    SurgeMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFare = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripFare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripFare_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<LineString>(type: "geometry (LineString, 4326)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRoutes_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripFare_TripId",
                table: "TripFare",
                column: "TripId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes",
                column: "TripId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripFare");

            migrationBuilder.DropTable(
                name: "TripRoutes");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ActualDistance",
                table: "Trips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ActualDuration",
                table: "Trips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Trips",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedDistance",
                table: "Trips",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedDuration",
                table: "Trips",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FinalFare_ActualDistance",
                table: "Trips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FinalFare_ActualTime",
                table: "Trips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalFare_CalculatedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalFare_FinalAmount",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripRequestId",
                table: "Trips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TripId",
                table: "LocationPings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
