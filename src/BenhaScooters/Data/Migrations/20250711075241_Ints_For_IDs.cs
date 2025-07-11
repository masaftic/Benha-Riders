using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Ints_For_IDs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
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
                });

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
                name: "IX_SmsVerificationCodes_Code",
                table: "SmsVerificationCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SmsVerificationCodes_UserId_PhoneNumber_CreatedAt",
                table: "SmsVerificationCodes",
                columns: new[] { "UserId", "PhoneNumber", "CreatedAt" });

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
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SmsVerificationCodes");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
