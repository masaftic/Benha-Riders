using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseTripRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverRatings");

            migrationBuilder.CreateTable(
                name: "TripRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    RiderId = table.Column<int>(type: "integer", nullable: false),
                    DriverRating = table.Column<int>(type: "integer", nullable: true),
                    DriverComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DriverRatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RiderRating = table.Column<int>(type: "integer", nullable: true),
                    RiderComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RiderRatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRatings_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripRatings_DriverId",
                table: "TripRatings",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRatings_RiderId",
                table: "TripRatings",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRatings_TripId",
                table: "TripRatings",
                column: "TripId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripRatings");

            migrationBuilder.CreateTable(
                name: "DriverRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    RiderId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverRatings_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverRatings_DriverId",
                table: "DriverRatings",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverRatings_TripId_RiderId",
                table: "DriverRatings",
                columns: new[] { "TripId", "RiderId" },
                unique: true);
        }
    }
}
