using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Relations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_DriverProfiles_DriverProfileId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationPings_Trips_TripId",
                table: "LocationPings");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                table: "TripRequests");

            migrationBuilder.DropIndex(
                name: "IX_DriverLocations_DriverProfileId",
                table: "DriverLocations");

            migrationBuilder.DropColumn(
                name: "Rating_CreatedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Rating_DriverComment",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Rating_DriverRating",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Rating_RiderComment",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Rating_RiderRating",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DriverProfileId",
                table: "DriverLocations");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverLocations_DriverProfiles_DriverId",
                table: "DriverLocations",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationPings_DriverProfiles_DriverId",
                table: "LocationPings",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationPings_Trips_TripId",
                table: "LocationPings",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                table: "TripRequests",
                column: "AssignedDriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_DriverProfiles_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationPings_DriverProfiles_DriverId",
                table: "LocationPings");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationPings_Trips_TripId",
                table: "LocationPings");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                table: "TripRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "Rating_CreatedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rating_DriverComment",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating_DriverRating",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rating_RiderComment",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating_RiderRating",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DriverProfileId",
                table: "DriverLocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_DriverProfileId",
                table: "DriverLocations",
                column: "DriverProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverLocations_DriverProfiles_DriverProfileId",
                table: "DriverLocations",
                column: "DriverProfileId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationPings_Trips_TripId",
                table: "LocationPings",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                table: "TripRequests",
                column: "AssignedDriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id");
        }
    }
}
