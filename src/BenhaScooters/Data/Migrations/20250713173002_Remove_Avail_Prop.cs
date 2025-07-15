using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Avail_Prop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverAvailabilities_IsAcceptingRequests",
                table: "DriverAvailabilities");

            migrationBuilder.DropColumn(
                name: "IsAcceptingRequests",
                table: "DriverAvailabilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAcceptingRequests",
                table: "DriverAvailabilities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_IsAcceptingRequests",
                table: "DriverAvailabilities",
                column: "IsAcceptingRequests");
        }
    }
}
