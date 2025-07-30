using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trip_Payment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TripPayment_Amount",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripPayment_ExternalReference",
                table: "Trips",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripPayment_Method",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TripPayment_PaidAmount",
                table: "Trips",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripPayment_Status",
                table: "Trips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TripPayment_Amount",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripPayment_ExternalReference",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripPayment_Method",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripPayment_PaidAmount",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripPayment_Status",
                table: "Trips");
        }
    }
}
