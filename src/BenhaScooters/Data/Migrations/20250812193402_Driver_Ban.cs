using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Driver_Ban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OnboardingState_RejectionReason",
                table: "Drivers",
                newName: "OnboardingState_BanReason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OnboardingState_BanReason",
                table: "Drivers",
                newName: "OnboardingState_RejectionReason");
        }
    }
}
