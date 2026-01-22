using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchAttemptRound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchingRound",
                table: "DriverMatchAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchingRound",
                table: "DriverMatchAttempts");
        }
    }
}
