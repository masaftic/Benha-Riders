using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailNormalized = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhoneNumberNormalized = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhoneNumberVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PersonalInfo_FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_NationalId = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    PersonalInfo_Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PersonalInfo_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_EmergencyContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    VehicleInfo_VehicleType = table.Column<string>(type: "text", nullable: true),
                    VehicleInfo_Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VehicleInfo_Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VehicleInfo_Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VehicleInfo_LicensePlate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VehicleInfo_Year = table.Column<int>(type: "integer", nullable: true),
                    Documents_LicenseImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Documents_VehicleRegistrationImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Documents_ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OnboardingStatus = table.Column<string>(type: "text", nullable: false),
                    CurrentStep = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drivers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Riders",
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
                    table.PrimaryKey("PK_Riders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Riders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmsVerificationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsVerificationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsVerificationCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Heading = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Speed = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLocations_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverRating", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverRating_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
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
                    EstimatedFare_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedFare_Distance = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedFare_Time = table.Column<double>(type: "double precision", nullable: false),
                    AssignedDriverId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRequests_Drivers_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TripRequests_Riders_RiderId",
                        column: x => x.RiderId,
                        principalTable: "Riders",
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
                        name: "FK_DriverMatchAttempts_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
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
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    RiderId = table.Column<int>(type: "integer", nullable: false),
                    TripRequestId = table.Column<int>(type: "integer", nullable: true),
                    PickupLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    DropoffLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DriverArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedFare_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedFare_Distance = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedFare_Time = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_Riders_RiderId",
                        column: x => x.RiderId,
                        principalTable: "Riders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_TripRequests_TripRequestId",
                        column: x => x.TripRequestId,
                        principalTable: "TripRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                    CurrentTripId = table.Column<int>(type: "integer", nullable: true),
                    OnlineSessionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalOnlineTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverAvailabilities_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverAvailabilities_Trips_CurrentTripId",
                        column: x => x.CurrentTripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TripFare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    BaseFare = table.Column<decimal>(type: "numeric", nullable: false),
                    DistanceFare = table.Column<decimal>(type: "numeric", nullable: false),
                    TimeFare = table.Column<decimal>(type: "numeric", nullable: false),
                    SurgeMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFare = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripFare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripFare_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripGpsPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Heading = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Speed = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripGpsPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripGpsPoints_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripGpsPoints_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<LineString>(type: "geometry (LineString, 4326)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRoutes_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_CurrentTripId",
                table: "DriverAvailabilities",
                column: "CurrentTripId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_DriverId",
                table: "DriverAvailabilities",
                column: "DriverId",
                unique: true);

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
                name: "IX_DriverRating_DriverId",
                table: "DriverRating",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_IsActive",
                table: "Drivers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_OnboardingStatus",
                table: "Drivers",
                column: "OnboardingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_PersonalInfo_NationalId",
                table: "Drivers",
                column: "PersonalInfo_NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_VehicleInfo_LicensePlate",
                table: "Drivers",
                column: "VehicleInfo_LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Riders_UserId",
                table: "Riders",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsVerificationCodes_Code",
                table: "SmsVerificationCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SmsVerificationCodes_UserId_PhoneNumber_CreatedAt",
                table: "SmsVerificationCodes",
                columns: new[] { "UserId", "PhoneNumber", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripFare_TripId",
                table: "TripFare",
                column: "TripId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripGpsPoints_DriverId",
                table: "TripGpsPoints",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TripGpsPoints_Location",
                table: "TripGpsPoints",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_TripGpsPoints_Timestamp",
                table: "TripGpsPoints",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TripGpsPoints_TripId",
                table: "TripGpsPoints",
                column: "TripId");

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
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes",
                column: "TripId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TripRequestId",
                table: "Trips",
                column: "TripRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailNormalized",
                table: "Users",
                column: "EmailNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumberNormalized",
                table: "Users",
                column: "PhoneNumberNormalized");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverAvailabilities");

            migrationBuilder.DropTable(
                name: "DriverLocations");

            migrationBuilder.DropTable(
                name: "DriverMatchAttempts");

            migrationBuilder.DropTable(
                name: "DriverRating");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SmsVerificationCodes");

            migrationBuilder.DropTable(
                name: "TripFare");

            migrationBuilder.DropTable(
                name: "TripGpsPoints");

            migrationBuilder.DropTable(
                name: "TripRoutes");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "TripRequests");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Riders");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
