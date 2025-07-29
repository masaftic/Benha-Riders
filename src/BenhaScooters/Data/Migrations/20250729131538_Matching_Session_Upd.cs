using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Matching_Session_Upd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "MatchingSessions");

            migrationBuilder.DropColumn(
                name: "CurrentPhaseAttempts",
                table: "MatchingSessions");

            migrationBuilder.DropColumn(
                name: "RejectionCount",
                table: "MatchingSessions");

            migrationBuilder.RenameColumn(
                name: "TotalAttempts",
                table: "MatchingSessions",
                newName: "NumberOfRounds");

            migrationBuilder.RenameColumn(
                name: "TimeoutCount",
                table: "MatchingSessions",
                newName: "CurrentRound");

            migrationBuilder.RenameColumn(
                name: "CurrentMode",
                table: "MatchingSessions",
                newName: "OffersPerRound");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OffersPerRound",
                table: "MatchingSessions",
                newName: "CurrentMode");

            migrationBuilder.RenameColumn(
                name: "NumberOfRounds",
                table: "MatchingSessions",
                newName: "TotalAttempts");

            migrationBuilder.RenameColumn(
                name: "CurrentRound",
                table: "MatchingSessions",
                newName: "TimeoutCount");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPhase",
                table: "MatchingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentPhaseAttempts",
                table: "MatchingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RejectionCount",
                table: "MatchingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
