using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class RmDeadTripFare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
