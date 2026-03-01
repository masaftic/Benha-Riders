using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class SmsSendLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sms_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provider_uid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sms_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_sms_logs_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_created_at",
                table: "sms_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_phone_number",
                table: "sms_logs",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_provider_id",
                table: "sms_logs",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_status",
                table: "sms_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_user_id",
                table: "sms_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sms_logs");
        }
    }
}
