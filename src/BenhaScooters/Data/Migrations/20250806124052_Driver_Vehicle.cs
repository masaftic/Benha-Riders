using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Driver_Vehicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverVehicle_Drivers_DriverId",
                table: "DriverVehicle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverVehicle",
                table: "DriverVehicle");

            migrationBuilder.DropIndex(
                name: "IX_DriverVehicle_DriverId",
                table: "DriverVehicle");

            migrationBuilder.RenameTable(
                name: "DriverVehicle",
                newName: "Vehicles");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ExpiryDate",
                table: "DriverDocument",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DriverId",
                table: "Vehicles",
                column: "DriverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Drivers_DriverId",
                table: "Vehicles",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Drivers_DriverId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_DriverId",
                table: "Vehicles");

            migrationBuilder.RenameTable(
                name: "Vehicles",
                newName: "DriverVehicle");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "DriverDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverVehicle",
                table: "DriverVehicle",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicle_DriverId",
                table: "DriverVehicle",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverVehicle_Drivers_DriverId",
                table: "DriverVehicle",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
