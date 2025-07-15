using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class EstimatedFare_Name_Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedFare_EstimatedTime",
                table: "Trips",
                newName: "EstimatedFare_Time");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_EstimatedAmount",
                table: "Trips",
                newName: "EstimatedFare_Amount");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_EstimatedTime",
                table: "TripRequests",
                newName: "EstimatedFare_Time");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_EstimatedAmount",
                table: "TripRequests",
                newName: "EstimatedFare_Amount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Time",
                table: "Trips",
                newName: "EstimatedFare_EstimatedTime");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Amount",
                table: "Trips",
                newName: "EstimatedFare_EstimatedAmount");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Time",
                table: "TripRequests",
                newName: "EstimatedFare_EstimatedTime");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Amount",
                table: "TripRequests",
                newName: "EstimatedFare_EstimatedAmount");
        }
    }
}
