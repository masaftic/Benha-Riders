using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class RmVehicleModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Vehicle_Model",
                table: "DriverProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Vehicle_Model",
                table: "DriverProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
