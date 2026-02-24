using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class TripRequestRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripRequestId",
                table: "Trips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TripRequestId",
                table: "Trips",
                column: "TripRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_TripRequests_TripRequestId",
                table: "Trips",
                column: "TripRequestId",
                principalTable: "TripRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_TripRequests_TripRequestId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_TripRequestId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripRequestId",
                table: "Trips");
        }
    }
}
