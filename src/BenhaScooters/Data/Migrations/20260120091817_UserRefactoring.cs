using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_Drivers_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverMatchAttempts_Drivers_DriverId",
                table: "DriverMatchAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_Drivers_MatchedDriverId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_Riders_RiderId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Drivers_DriverId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Riders_RiderId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "DriverAvailabilities");

            migrationBuilder.DropTable(
                name: "DriverDocument");

            migrationBuilder.DropTable(
                name: "DriverField");

            migrationBuilder.DropTable(
                name: "DriverRatings");

            migrationBuilder.DropTable(
                name: "Riders");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Time",
                table: "Trips",
                newName: "FinalFare_Time");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Distance",
                table: "Trips",
                newName: "FinalFare_Distance");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Amount",
                table: "Trips",
                newName: "FinalFare_Amount");

            migrationBuilder.RenameColumn(
                name: "RiderId",
                table: "Trips",
                newName: "RiderUserId");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "Trips",
                newName: "DriverUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_RiderId",
                table: "Trips",
                newName: "IX_Trips_RiderUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_DriverId",
                table: "Trips",
                newName: "IX_Trips_DriverUserId");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Time",
                table: "TripRequests",
                newName: "FinalFare_Time");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Distance",
                table: "TripRequests",
                newName: "FinalFare_Distance");

            migrationBuilder.RenameColumn(
                name: "EstimatedFare_Amount",
                table: "TripRequests",
                newName: "FinalFare_Amount");

            migrationBuilder.RenameColumn(
                name: "RiderId",
                table: "TripRequests",
                newName: "RiderUserId");

            migrationBuilder.RenameColumn(
                name: "MatchedDriverId",
                table: "TripRequests",
                newName: "MatchedDriverUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TripRequests_RiderId",
                table: "TripRequests",
                newName: "IX_TripRequests_RiderUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TripRequests_MatchedDriverId",
                table: "TripRequests",
                newName: "IX_TripRequests_MatchedDriverUserId");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "DriverMatchAttempts",
                newName: "DriverUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DriverMatchAttempts_DriverId",
                table: "DriverMatchAttempts",
                newName: "IX_DriverMatchAttempts_DriverUserId");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "DriverLocations",
                newName: "UserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalFare_Amount",
                table: "TripRequests",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<float>(
                name: "Heading",
                table: "DriverLocations",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Speed",
                table: "DriverLocations",
                type: "real",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocuments_DriverUserId_Type",
                table: "DriverDocuments",
                columns: new[] { "DriverUserId", "Type" },
                unique: true);

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
                name: "IX_RiderSavedAddresses_RiderUserId_Label",
                table: "RiderSavedAddresses",
                columns: new[] { "RiderUserId", "Label" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverLocations_Users_UserId",
                table: "DriverLocations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverMatchAttempts_DriverProfiles_DriverUserId",
                table: "DriverMatchAttempts",
                column: "DriverUserId",
                principalTable: "DriverProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_DriverProfiles_MatchedDriverUserId",
                table: "TripRequests",
                column: "MatchedDriverUserId",
                principalTable: "DriverProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_RiderProfiles_RiderUserId",
                table: "TripRequests",
                column: "RiderUserId",
                principalTable: "RiderProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_DriverProfiles_DriverUserId",
                table: "Trips",
                column: "DriverUserId",
                principalTable: "DriverProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_RiderProfiles_RiderUserId",
                table: "Trips",
                column: "RiderUserId",
                principalTable: "RiderProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_Users_UserId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverMatchAttempts_DriverProfiles_DriverUserId",
                table: "DriverMatchAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_DriverProfiles_MatchedDriverUserId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_RiderProfiles_RiderUserId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_DriverProfiles_DriverUserId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_RiderProfiles_RiderUserId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "DriverDocuments");

            migrationBuilder.DropTable(
                name: "DriverStats");

            migrationBuilder.DropTable(
                name: "DriverStatuses");

            migrationBuilder.DropTable(
                name: "RiderSavedAddresses");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "Heading",
                table: "DriverLocations");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "DriverLocations");

            migrationBuilder.RenameColumn(
                name: "FinalFare_Time",
                table: "Trips",
                newName: "EstimatedFare_Time");

            migrationBuilder.RenameColumn(
                name: "FinalFare_Distance",
                table: "Trips",
                newName: "EstimatedFare_Distance");

            migrationBuilder.RenameColumn(
                name: "FinalFare_Amount",
                table: "Trips",
                newName: "EstimatedFare_Amount");

            migrationBuilder.RenameColumn(
                name: "RiderUserId",
                table: "Trips",
                newName: "RiderId");

            migrationBuilder.RenameColumn(
                name: "DriverUserId",
                table: "Trips",
                newName: "DriverId");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_RiderUserId",
                table: "Trips",
                newName: "IX_Trips_RiderId");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_DriverUserId",
                table: "Trips",
                newName: "IX_Trips_DriverId");

            migrationBuilder.RenameColumn(
                name: "FinalFare_Time",
                table: "TripRequests",
                newName: "EstimatedFare_Time");

            migrationBuilder.RenameColumn(
                name: "FinalFare_Distance",
                table: "TripRequests",
                newName: "EstimatedFare_Distance");

            migrationBuilder.RenameColumn(
                name: "FinalFare_Amount",
                table: "TripRequests",
                newName: "EstimatedFare_Amount");

            migrationBuilder.RenameColumn(
                name: "RiderUserId",
                table: "TripRequests",
                newName: "RiderId");

            migrationBuilder.RenameColumn(
                name: "MatchedDriverUserId",
                table: "TripRequests",
                newName: "MatchedDriverId");

            migrationBuilder.RenameIndex(
                name: "IX_TripRequests_RiderUserId",
                table: "TripRequests",
                newName: "IX_TripRequests_RiderId");

            migrationBuilder.RenameIndex(
                name: "IX_TripRequests_MatchedDriverUserId",
                table: "TripRequests",
                newName: "IX_TripRequests_MatchedDriverId");

            migrationBuilder.RenameColumn(
                name: "DriverUserId",
                table: "DriverMatchAttempts",
                newName: "DriverId");

            migrationBuilder.RenameIndex(
                name: "IX_DriverMatchAttempts_DriverUserId",
                table: "DriverMatchAttempts",
                newName: "IX_DriverMatchAttempts_DriverId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "DriverLocations",
                newName: "DriverId");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedFare_Amount",
                table: "TripRequests",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.CreateTable(
                name: "DriverAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrentTripId = table.Column<int>(type: "integer", nullable: true),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    LastStatusChange = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OnlineSessionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalOnlineTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverAvailabilities_Trips_CurrentTripId",
                        column: x => x.CurrentTripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Info_Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Info_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Info_DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Info_EmergencyContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Info_EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    Info_FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Info_NationalId = table.Column<string>(type: "text", nullable: true),
                    OnboardingState_BanReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OnboardingState_CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OnboardingState_CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OnboardingState_Status = table.Column<string>(type: "text", nullable: false)
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
                name: "Riders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DefaultPaymentMethodId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastTripAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SavedAddresses = table.Column<string>(type: "text", nullable: false),
                    TotalTrips = table.Column<int>(type: "integer", nullable: false),
                    Rating_Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    Rating_TotalRatings = table.Column<int>(type: "integer", nullable: false)
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
                name: "DriverDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDocument_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverField",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Step = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverField_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverRatings",
                columns: table => new
                {
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverRatings", x => x.DriverId);
                    table.ForeignKey(
                        name: "FK_DriverRatings_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LicensePlate = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VIN = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    VehicleType = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_CurrentTripId",
                table: "DriverAvailabilities",
                column: "CurrentTripId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_Status",
                table: "DriverAvailabilities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocument_DriverId",
                table: "DriverDocument",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverField_DriverId",
                table: "DriverField",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_Info_NationalId",
                table: "Drivers",
                column: "Info_NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_IsActive",
                table: "Drivers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Riders_UserId",
                table: "Riders",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DriverId",
                table: "Vehicles",
                column: "DriverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverLocations_Drivers_DriverId",
                table: "DriverLocations",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverMatchAttempts_Drivers_DriverId",
                table: "DriverMatchAttempts",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_Drivers_MatchedDriverId",
                table: "TripRequests",
                column: "MatchedDriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_Riders_RiderId",
                table: "TripRequests",
                column: "RiderId",
                principalTable: "Riders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Drivers_DriverId",
                table: "Trips",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Riders_RiderId",
                table: "Trips",
                column: "RiderId",
                principalTable: "Riders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
