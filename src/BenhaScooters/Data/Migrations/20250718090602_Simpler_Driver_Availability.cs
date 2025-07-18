using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Simpler_Driver_Availability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverAvailabilities_LastLocationUpdate",
                table: "DriverAvailabilities");

            migrationBuilder.DropColumn(
                name: "LastLocationUpdate",
                table: "DriverAvailabilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationUpdate",
                table: "DriverAvailabilities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_LastLocationUpdate",
                table: "DriverAvailabilities",
                column: "LastLocationUpdate");
        }
    }
}
