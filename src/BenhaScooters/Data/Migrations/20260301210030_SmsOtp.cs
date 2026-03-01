using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class SmsOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SmsVerificationCodes_Code",
                table: "SmsVerificationCodes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "SmsVerificationCodes");

            migrationBuilder.AddColumn<string>(
                name: "CodeHash",
                table: "SmsVerificationCodes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "SmsVerificationCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "SmsVerificationCodes",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "SmsVerificationCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OtpSecurityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpSecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmsVerificationCodes_UserId_IsUsed_ExpiresAt",
                table: "SmsVerificationCodes",
                columns: new[] { "UserId", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpSecurityEvents_CreatedAt",
                table: "OtpSecurityEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OtpSecurityEvents_IpAddress_EventType_CreatedAt",
                table: "OtpSecurityEvents",
                columns: new[] { "IpAddress", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpSecurityEvents_PhoneNumber_EventType_CreatedAt",
                table: "OtpSecurityEvents",
                columns: new[] { "PhoneNumber", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpSecurityEvents_UserId_EventType_CreatedAt",
                table: "OtpSecurityEvents",
                columns: new[] { "UserId", "EventType", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpSecurityEvents");

            migrationBuilder.DropIndex(
                name: "IX_SmsVerificationCodes_UserId_IsUsed_ExpiresAt",
                table: "SmsVerificationCodes");

            migrationBuilder.DropColumn(
                name: "CodeHash",
                table: "SmsVerificationCodes");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "SmsVerificationCodes");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "SmsVerificationCodes");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "SmsVerificationCodes");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SmsVerificationCodes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SmsVerificationCodes_Code",
                table: "SmsVerificationCodes",
                column: "Code");
        }
    }
}
