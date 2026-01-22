using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
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
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneNumberNormalized = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneNumberVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverLocations",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Heading = table.Column<float>(type: "real", nullable: true),
                    Speed = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLocations", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DriverLocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PersonalInfo_FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_NationalId = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    PersonalInfo_Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PersonalInfo_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_EmergencyContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    Vehicle_Type = table.Column<string>(type: "text", nullable: true),
                    Vehicle_Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Vehicle_Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Vehicle_Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Vehicle_LicensePlate = table.Column<string>(type: "text", nullable: true),
                    Vehicle_Year = table.Column<int>(type: "integer", nullable: true),
                    Vehicle_VIN = table.Column<string>(type: "text", nullable: true),
                    OnboardingStatus = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverStats",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    TotalTrips = table.Column<int>(type: "integer", nullable: false),
                    CompletedTrips = table.Column<int>(type: "integer", nullable: false),
                    CancelledTrips = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    TotalOnlineTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    LastTripAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastOnlineAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStreakDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverStats", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DriverStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverStatuses",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CurrentTripId = table.Column<int>(type: "integer", nullable: true),
                    LastStatusChange = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OnlineSessionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverStatuses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DriverStatuses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuths",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuths", x => new { x.Provider, x.ProviderUserId });
                    table.ForeignKey(
                        name: "FK_ExternalAuths_Users_UserId",
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
                name: "RiderProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultPaymentMethodId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    TotalTrips = table.Column<int>(type: "integer", nullable: false),
                    LastTripAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_Users_UserId",
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
                name: "DriverDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverUserId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDocuments_DriverProfiles_DriverUserId",
                        column: x => x.DriverUserId,
                        principalTable: "DriverProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiderSavedAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    RiderUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderSavedAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderSavedAddresses_RiderProfiles_RiderUserId",
                        column: x => x.RiderUserId,
                        principalTable: "RiderProfiles",
                        principalColumn: "UserId",
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
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalFare_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FinalFare_Distance = table.Column<double>(type: "double precision", nullable: false),
                    FinalFare_Time = table.Column<double>(type: "double precision", nullable: false),
                    MatchedDriverId = table.Column<int>(type: "integer", nullable: true),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRequests_DriverProfiles_MatchedDriverId",
                        column: x => x.MatchedDriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TripRequests_RiderProfiles_RiderId",
                        column: x => x.RiderId,
                        principalTable: "RiderProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    RiderId = table.Column<int>(type: "integer", nullable: false),
                    PickupLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    DropoffLocation = table.Column<Point>(type: "geography (point)", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DriverArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalFare_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FinalFare_Distance = table.Column<double>(type: "double precision", nullable: false),
                    FinalFare_Time = table.Column<double>(type: "double precision", nullable: false),
                    TripFare_BaseFare = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TripFare_DistanceFare = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TripFare_DurationFare = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TripFare_SurgeMultiplier = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalFare = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TripPayment_Method = table.Column<string>(type: "text", nullable: true),
                    TripPayment_Status = table.Column<string>(type: "text", nullable: true),
                    TripPayment_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TripPayment_PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TripPayment_ExternalReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_DriverProfiles_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriverProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_RiderProfiles_RiderId",
                        column: x => x.RiderId,
                        principalTable: "RiderProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripRequestId = table.Column<int>(type: "integer", nullable: false),
                    NumberOfRounds = table.Column<int>(type: "integer", nullable: false),
                    OffersPerRound = table.Column<int[]>(type: "integer[]", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TripRoutes",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<LineString>(type: "geometry (LineString, 4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRoutes", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_TripRoutes_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverMatchAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchingSessionId = table.Column<int>(type: "integer", nullable: false),
                    DriverUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MatchingRound = table.Column<int>(type: "integer", nullable: false),
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
                        name: "FK_DriverMatchAttempts_DriverProfiles_DriverUserId",
                        column: x => x.DriverUserId,
                        principalTable: "DriverProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverMatchAttempts_MatchingSessions_MatchingSessionId",
                        column: x => x.MatchingSessionId,
                        principalTable: "MatchingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripGpsPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripGpsPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripGpsPoints_TripRoutes_TripId",
                        column: x => x.TripId,
                        principalTable: "TripRoutes",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocuments_DriverUserId_Type",
                table: "DriverDocuments",
                columns: new[] { "DriverUserId", "Type" },
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
                name: "IX_DriverMatchAttempts_DriverUserId",
                table: "DriverMatchAttempts",
                column: "DriverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_MatchingSessionId",
                table: "DriverMatchAttempts",
                column: "MatchingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverMatchAttempts_Status",
                table: "DriverMatchAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_OnboardingStatus",
                table: "DriverProfiles",
                column: "OnboardingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DriverStats_AverageRating",
                table: "DriverStats",
                column: "AverageRating");

            migrationBuilder.CreateIndex(
                name: "IX_DriverStats_CompletedTrips",
                table: "DriverStats",
                column: "CompletedTrips");

            migrationBuilder.CreateIndex(
                name: "IX_DriverStatuses_Status",
                table: "DriverStatuses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverStatuses_Status_UserId",
                table: "DriverStatuses",
                columns: new[] { "Status", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuths_UserId",
                table: "ExternalAuths",
                column: "UserId");

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
                name: "IX_RiderSavedAddresses_RiderUserId_Label",
                table: "RiderSavedAddresses",
                columns: new[] { "RiderUserId", "Label" },
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
                name: "IX_TripRequests_MatchedDriverId",
                table: "TripRequests",
                column: "MatchedDriverId");

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
                name: "DriverDocuments");

            migrationBuilder.DropTable(
                name: "DriverLocations");

            migrationBuilder.DropTable(
                name: "DriverMatchAttempts");

            migrationBuilder.DropTable(
                name: "DriverStats");

            migrationBuilder.DropTable(
                name: "DriverStatuses");

            migrationBuilder.DropTable(
                name: "ExternalAuths");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RiderSavedAddresses");

            migrationBuilder.DropTable(
                name: "SmsVerificationCodes");

            migrationBuilder.DropTable(
                name: "TripGpsPoints");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "MatchingSessions");

            migrationBuilder.DropTable(
                name: "TripRoutes");

            migrationBuilder.DropTable(
                name: "TripRequests");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "RiderProfiles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
