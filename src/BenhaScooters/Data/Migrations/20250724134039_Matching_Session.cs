using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Matching_Session : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_Drivers_AssignedDriverId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_TripRequests_TripRequestId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_TripRequestId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripRequestId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "TripRequests");

            migrationBuilder.RenameColumn(
                name: "AssignedDriverId",
                table: "TripRequests",
                newName: "MatchedDriverId");

            migrationBuilder.RenameColumn(
                name: "AssignedAt",
                table: "TripRequests",
                newName: "MatchedAt");

            migrationBuilder.RenameIndex(
                name: "IX_TripRequests_AssignedDriverId",
                table: "TripRequests",
                newName: "IX_TripRequests_MatchedDriverId");

            migrationBuilder.AddColumn<int>(
                name: "MatchingSessionId",
                table: "DriverMatchAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MatchingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripRequestId = table.Column<int>(type: "integer", nullable: false),
                    CurrentMode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAttempts = table.Column<int>(type: "integer", nullable: false),
                    RejectionCount = table.Column<int>(type: "integer", nullable: false),
                    TimeoutCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingSessions_TripRequests_TripRequestId",
                        column: x => x.TripRequestId,
                        principalTable: "TripRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_MatchingSessionId",
                table: "DriverMatchAttempts",
                column: "MatchingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingSessions_CreatedAt",
                table: "MatchingSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingSessions_Status",
                table: "MatchingSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingSessions_TripRequestId",
                table: "MatchingSessions",
                column: "TripRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverMatchAttempts_MatchingSessions_MatchingSessionId",
                table: "DriverMatchAttempts",
                column: "MatchingSessionId",
                principalTable: "MatchingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_Drivers_MatchedDriverId",
                table: "TripRequests",
                column: "MatchedDriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverMatchAttempts_MatchingSessions_MatchingSessionId",
                table: "DriverMatchAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_Drivers_MatchedDriverId",
                table: "TripRequests");

            migrationBuilder.DropTable(
                name: "MatchingSessions");

            migrationBuilder.DropIndex(
                name: "IX_DriverMatchAttempts_MatchingSessionId",
                table: "DriverMatchAttempts");

            migrationBuilder.DropColumn(
                name: "MatchingSessionId",
                table: "DriverMatchAttempts");

            migrationBuilder.RenameColumn(
                name: "MatchedDriverId",
                table: "TripRequests",
                newName: "AssignedDriverId");

            migrationBuilder.RenameColumn(
                name: "MatchedAt",
                table: "TripRequests",
                newName: "AssignedAt");

            migrationBuilder.RenameIndex(
                name: "IX_TripRequests_MatchedDriverId",
                table: "TripRequests",
                newName: "IX_TripRequests_AssignedDriverId");

            migrationBuilder.AddColumn<int>(
                name: "TripRequestId",
                table: "Trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "TripRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TripRequestId",
                table: "Trips",
                column: "TripRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_Drivers_AssignedDriverId",
                table: "TripRequests",
                column: "AssignedDriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_TripRequests_TripRequestId",
                table: "Trips",
                column: "TripRequestId",
                principalTable: "TripRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
