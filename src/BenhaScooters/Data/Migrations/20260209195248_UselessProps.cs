using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class UselessProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmailNormalized",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumberNormalized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailNormalized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberNormalized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalInfo_Address",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "PersonalInfo_City",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "PersonalInfo_DateOfBirth",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "PersonalInfo_EmergencyContactName",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "PersonalInfo_EmergencyContactPhone",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "Vehicle_VIN",
                table: "DriverProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "EmailNormalized",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumberNormalized",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalInfo_Address",
                table: "DriverProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalInfo_City",
                table: "DriverProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PersonalInfo_DateOfBirth",
                table: "DriverProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalInfo_EmergencyContactName",
                table: "DriverProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalInfo_EmergencyContactPhone",
                table: "DriverProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vehicle_VIN",
                table: "DriverProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailNormalized",
                table: "Users",
                column: "EmailNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumberNormalized",
                table: "Users",
                column: "PhoneNumberNormalized");
        }
    }
}
