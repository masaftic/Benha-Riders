using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class TripRoute_GpsPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripGpsPoints_Trips_TripId",
                table: "TripGpsPoints");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TripRoutes");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "TripRoutes");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "TripGpsPoints",
                newName: "TripRouteId");

            migrationBuilder.RenameIndex(
                name: "IX_TripGpsPoints_TripId",
                table: "TripGpsPoints",
                newName: "IX_TripGpsPoints_TripRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_TripGpsPoints_TripRoutes_TripRouteId",
                table: "TripGpsPoints",
                column: "TripRouteId",
                principalTable: "TripRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripGpsPoints_TripRoutes_TripRouteId",
                table: "TripGpsPoints");

            migrationBuilder.RenameColumn(
                name: "TripRouteId",
                table: "TripGpsPoints",
                newName: "TripId");

            migrationBuilder.RenameIndex(
                name: "IX_TripGpsPoints_TripRouteId",
                table: "TripGpsPoints",
                newName: "IX_TripGpsPoints_TripId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TripRoutes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "TripRoutes",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddForeignKey(
                name: "FK_TripGpsPoints_Trips_TripId",
                table: "TripGpsPoints",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
