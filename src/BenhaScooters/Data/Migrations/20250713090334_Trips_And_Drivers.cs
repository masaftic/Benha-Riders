using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trips_And_Drivers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverProfiles_IsOnline",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "DriverProfiles");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "DriverAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastStatusChange = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLocationUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAcceptingRequests = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentTripId = table.Column<int>(type: "integer", nullable: true),
                    OnlineSessionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalOnlineTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverAvailabilities_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    DriverProfileId = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Heading = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Speed = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLocations_DriverProfiles_DriverProfileId",
                        column: x => x.DriverProfileId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiderProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Rating_Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    Rating_TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    DefaultPaymentMethodId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SavedAddresses = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTripAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalTrips = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RiderId = table.Column<int>(type: "integer", nullable: false),
                    PickupLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    DropoffLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedFare_EstimatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedFare_Distance = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedFare_EstimatedTime = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedDistance = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedDuration = table.Column<double>(type: "double precision", nullable: false),
                    AssignedDriverId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripRequests_RiderProfiles_RiderId",
                        column: x => x.RiderId,
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripRequestId = table.Column<int>(type: "integer", nullable: false),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    RiderId = table.Column<int>(type: "integer", nullable: false),
                    PickupLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    DropoffLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriverArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstimatedFare_EstimatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedFare_Distance = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedFare_EstimatedTime = table.Column<double>(type: "double precision", nullable: false),
                    FinalFare_FinalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    FinalFare_ActualDistance = table.Column<double>(type: "double precision", nullable: true),
                    FinalFare_ActualTime = table.Column<double>(type: "double precision", nullable: true),
                    FinalFare_CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDistance = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedDuration = table.Column<double>(type: "double precision", nullable: false),
                    ActualDistance = table.Column<double>(type: "double precision", nullable: true),
                    ActualDuration = table.Column<double>(type: "double precision", nullable: true),
                    Rating_DriverRating = table.Column<decimal>(type: "numeric", nullable: true),
                    Rating_RiderRating = table.Column<decimal>(type: "numeric", nullable: true),
                    Rating_DriverComment = table.Column<string>(type: "text", nullable: true),
                    Rating_RiderComment = table.Column<string>(type: "text", nullable: true),
                    Rating_CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_RiderProfiles_RiderId",
                        column: x => x.RiderId,
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverMatchAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripRequestId = table.Column<int>(type: "integer", nullable: false),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DistanceToPickup = table.Column<double>(type: "double precision", precision: 8, scale: 3, nullable: false),
                    EstimatedArrivalTime = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    DriverScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverMatchAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverMatchAttempts_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverMatchAttempts_TripRequests_TripRequestId",
                        column: x => x.TripRequestId,
                        principalTable: "TripRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationPings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Heading = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Speed = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationPings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationPings_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_DriverId",
                table: "DriverAvailabilities",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_IsAcceptingRequests",
                table: "DriverAvailabilities",
                column: "IsAcceptingRequests");

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_LastLocationUpdate",
                table: "DriverAvailabilities",
                column: "LastLocationUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_Status",
                table: "DriverAvailabilities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_DriverId",
                table: "DriverLocations",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_DriverProfileId",
                table: "DriverLocations",
                column: "DriverProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_Location",
                table: "DriverLocations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_Timestamp",
                table: "DriverLocations",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_CreatedAt",
                table: "DriverMatchAttempts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_DriverId",
                table: "DriverMatchAttempts",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_Status",
                table: "DriverMatchAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_TripRequestId",
                table: "DriverMatchAttempts",
                column: "TripRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPings_DriverId",
                table: "LocationPings",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPings_Location",
                table: "LocationPings",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPings_Timestamp",
                table: "LocationPings",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPings_TripId",
                table: "LocationPings",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_UserId",
                table: "RiderProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_AssignedDriverId",
                table: "TripRequests",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_RequestedAt",
                table: "TripRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_RiderId",
                table: "TripRequests",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_Status",
                table: "TripRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CreatedAt",
                table: "Trips",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId",
                table: "Trips",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RiderId",
                table: "Trips",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status",
                table: "Trips",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverProfiles_Users_UserId",
                table: "DriverProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverProfiles_Users_UserId",
                table: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "DriverAvailabilities");

            migrationBuilder.DropTable(
                name: "DriverLocations");

            migrationBuilder.DropTable(
                name: "DriverMatchAttempts");

            migrationBuilder.DropTable(
                name: "LocationPings");

            migrationBuilder.DropTable(
                name: "TripRequests");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "RiderProfiles");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "DriverProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_IsOnline",
                table: "DriverProfiles",
                column: "IsOnline");
        }
    }
}
