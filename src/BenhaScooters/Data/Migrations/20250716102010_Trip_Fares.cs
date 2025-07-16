using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trip_Fares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripFare_Trips_TripId",
                table: "TripFare");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TripFare",
                table: "TripFare");

            migrationBuilder.RenameTable(
                name: "TripFare",
                newName: "TripFares");

            migrationBuilder.RenameIndex(
                name: "IX_TripFare_TripId",
                table: "TripFares",
                newName: "IX_TripFares_TripId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TripFares",
                table: "TripFares",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TripFares_Trips_TripId",
                table: "TripFares",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripFares_Trips_TripId",
                table: "TripFares");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TripFares",
                table: "TripFares");

            migrationBuilder.RenameTable(
                name: "TripFares",
                newName: "TripFare");

            migrationBuilder.RenameIndex(
                name: "IX_TripFares_TripId",
                table: "TripFare",
                newName: "IX_TripFare_TripId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TripFare",
                table: "TripFare",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TripFare_Trips_TripId",
                table: "TripFare",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
