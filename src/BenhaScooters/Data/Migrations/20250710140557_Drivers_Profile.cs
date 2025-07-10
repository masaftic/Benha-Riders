using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Drivers_Profile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonalInfo_FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonalInfo_NationalId = table.Column<string>(type: "text", nullable: true),
                    PersonalInfo_DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    Documents_ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Rating_Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    Rating_TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    OnboardingStatus = table.Column<string>(type: "text", nullable: false),
                    CurrentStep = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_IsActive",
                table: "DriverProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_IsOnline",
                table: "DriverProfiles",
                column: "IsOnline");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverProfiles");
        }
    }
}
