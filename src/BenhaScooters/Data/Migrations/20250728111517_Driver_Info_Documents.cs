using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class Driver_Info_Documents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverAvailabilities_Drivers_DriverId",
                table: "DriverAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverLocations_Drivers_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverRating_Drivers_DriverId",
                table: "DriverRating");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_OnboardingStatus",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_VehicleInfo_LicensePlate",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_DriverLocations_DriverId",
                table: "DriverLocations");

            migrationBuilder.DropIndex(
                name: "IX_DriverAvailabilities_DriverId",
                table: "DriverAvailabilities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverRating",
                table: "DriverRating");

            migrationBuilder.DropIndex(
                name: "IX_DriverRating_DriverId",
                table: "DriverRating");

            migrationBuilder.DropColumn(
                name: "Documents_ImageUrl",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "Documents_LicenseImageUrl",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "Documents_VehicleRegistrationImageUrl",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VehicleInfo_Brand",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VehicleInfo_Color",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VehicleInfo_LicensePlate",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VehicleInfo_Model",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VehicleInfo_VehicleType",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "VehicleInfo_Year",
                table: "Drivers");

            migrationBuilder.RenameTable(
                name: "DriverRating",
                newName: "DriverRatings");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "Drivers",
                newName: "OnboardingState_RejectionReason");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_NationalId",
                table: "Drivers",
                newName: "Info_NationalId");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_FullName",
                table: "Drivers",
                newName: "Info_FullName");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_EmergencyContactPhone",
                table: "Drivers",
                newName: "Info_EmergencyContactPhone");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_EmergencyContactName",
                table: "Drivers",
                newName: "Info_EmergencyContactName");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_DateOfBirth",
                table: "Drivers",
                newName: "Info_DateOfBirth");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_City",
                table: "Drivers",
                newName: "Info_City");

            migrationBuilder.RenameColumn(
                name: "PersonalInfo_Address",
                table: "Drivers",
                newName: "Info_Address");

            migrationBuilder.RenameColumn(
                name: "CurrentStep",
                table: "Drivers",
                newName: "OnboardingState_CurrentStep");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Drivers",
                newName: "OnboardingState_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "Drivers",
                newName: "OnboardingState_CompletedAt");

            migrationBuilder.RenameColumn(
                name: "OnboardingStatus",
                table: "Drivers",
                newName: "OnboardingState_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Drivers_PersonalInfo_NationalId",
                table: "Drivers",
                newName: "IX_Drivers_Info_NationalId");

            migrationBuilder.AlterColumn<string>(
                name: "OnboardingState_RejectionReason",
                table: "Drivers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverRatings",
                table: "DriverRatings",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DriverDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
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
                name: "DriverVehicle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    VehicleType = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LicensePlate = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    VIN = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverVehicle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverVehicle_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocument_DriverId",
                table: "DriverDocument",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicle_DriverId",
                table: "DriverVehicle",
                column: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverDocument");

            migrationBuilder.DropTable(
                name: "DriverVehicle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverRatings",
                table: "DriverRatings");

            migrationBuilder.RenameTable(
                name: "DriverRatings",
                newName: "DriverRating");

            migrationBuilder.RenameColumn(
                name: "OnboardingState_RejectionReason",
                table: "Drivers",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "OnboardingState_CurrentStep",
                table: "Drivers",
                newName: "CurrentStep");

            migrationBuilder.RenameColumn(
                name: "OnboardingState_CreatedAt",
                table: "Drivers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "OnboardingState_CompletedAt",
                table: "Drivers",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "Info_NationalId",
                table: "Drivers",
                newName: "PersonalInfo_NationalId");

            migrationBuilder.RenameColumn(
                name: "Info_FullName",
                table: "Drivers",
                newName: "PersonalInfo_FullName");

            migrationBuilder.RenameColumn(
                name: "Info_EmergencyContactPhone",
                table: "Drivers",
                newName: "PersonalInfo_EmergencyContactPhone");

            migrationBuilder.RenameColumn(
                name: "Info_EmergencyContactName",
                table: "Drivers",
                newName: "PersonalInfo_EmergencyContactName");

            migrationBuilder.RenameColumn(
                name: "Info_DateOfBirth",
                table: "Drivers",
                newName: "PersonalInfo_DateOfBirth");

            migrationBuilder.RenameColumn(
                name: "Info_City",
                table: "Drivers",
                newName: "PersonalInfo_City");

            migrationBuilder.RenameColumn(
                name: "Info_Address",
                table: "Drivers",
                newName: "PersonalInfo_Address");

            migrationBuilder.RenameColumn(
                name: "OnboardingState_Status",
                table: "Drivers",
                newName: "OnboardingStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Drivers_Info_NationalId",
                table: "Drivers",
                newName: "IX_Drivers_PersonalInfo_NationalId");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Drivers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Documents_ImageUrl",
                table: "Drivers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Documents_LicenseImageUrl",
                table: "Drivers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Documents_VehicleRegistrationImageUrl",
                table: "Drivers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleInfo_Brand",
                table: "Drivers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleInfo_Color",
                table: "Drivers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleInfo_LicensePlate",
                table: "Drivers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleInfo_Model",
                table: "Drivers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleInfo_VehicleType",
                table: "Drivers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleInfo_Year",
                table: "Drivers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverRating",
                table: "DriverRating",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_OnboardingStatus",
                table: "Drivers",
                column: "OnboardingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_VehicleInfo_LicensePlate",
                table: "Drivers",
                column: "VehicleInfo_LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLocations_DriverId",
                table: "DriverLocations",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverAvailabilities_DriverId",
                table: "DriverAvailabilities",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverRating_DriverId",
                table: "DriverRating",
                column: "DriverId",
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
                name: "FK_DriverRating_Drivers_DriverId",
                table: "DriverRating",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
