using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Driver_Onb_Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingState_CurrentStep",
                table: "Drivers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OnboardingState_CurrentStep",
                table: "Drivers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
