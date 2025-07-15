using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Removed_Profiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverAvailabilities_DriverProfiles_DriverId",
                table: "DriverAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_DriverProfiles_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverMatchAttempts_DriverProfiles_DriverId",
                table: "DriverMatchAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationPings_DriverProfiles_DriverId",
                table: "LocationPings");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_RiderProfiles_RiderId",
                table: "TripRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_DriverProfiles_DriverId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_RiderProfiles_RiderId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "EstimatedDistance",
                table: "TripRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedDuration",
                table: "TripRequests");

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
                    Rating_Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    Rating_TotalRatings = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_Riders_UserId",
                table: "Riders",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverAvailabilities_Drivers_DriverId",
                table: "DriverAvailabilities",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_LocationPings_Drivers_DriverId",
                table: "LocationPings",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_Drivers_AssignedDriverId",
                table: "TripRequests",
                column: "AssignedDriverId",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverAvailabilities_Drivers_DriverId",
                table: "DriverAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_Drivers_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverMatchAttempts_Drivers_DriverId",
                table: "DriverMatchAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationPings_Drivers_DriverId",
                table: "LocationPings");

            migrationBuilder.DropForeignKey(
                name: "FK_TripRequests_Drivers_AssignedDriverId",
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
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Riders");

            migrationBuilder.AddColumn<double>(
                name: "EstimatedDistance",
                table: "TripRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedDuration",
                table: "TripRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentStep = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OnboardingStatus = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Documents_LicenseImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Documents_ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Documents_VehicleRegistrationImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PersonalInfo_Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PersonalInfo_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    PersonalInfo_EmergencyContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_NationalId = table.Column<string>(type: "text", nullable: true),
                    Rating_Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    Rating_TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    VehicleInfo_Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VehicleInfo_Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VehicleInfo_LicensePlate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VehicleInfo_Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VehicleInfo_VehicleType = table.Column<string>(type: "text", nullable: true),
                    VehicleInfo_Year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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
                    table.PrimaryKey("PK_RiderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_IsActive",
                table: "DriverProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_OnboardingStatus",
                table: "DriverProfiles",
                column: "OnboardingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_PersonalInfo_NationalId",
                table: "DriverProfiles",
                column: "PersonalInfo_NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_UserId",
                table: "DriverProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_VehicleInfo_LicensePlate",
                table: "DriverProfiles",
                column: "VehicleInfo_LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_UserId",
                table: "RiderProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverAvailabilities_DriverProfiles_DriverId",
                table: "DriverAvailabilities",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverLocations_DriverProfiles_DriverId",
                table: "DriverLocations",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverMatchAttempts_DriverProfiles_DriverId",
                table: "DriverMatchAttempts",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationPings_DriverProfiles_DriverId",
                table: "LocationPings",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_DriverProfiles_AssignedDriverId",
                table: "TripRequests",
                column: "AssignedDriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TripRequests_RiderProfiles_RiderId",
                table: "TripRequests",
                column: "RiderId",
                principalTable: "RiderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_DriverProfiles_DriverId",
                table: "Trips",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_RiderProfiles_RiderId",
                table: "Trips",
                column: "RiderId",
                principalTable: "RiderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
