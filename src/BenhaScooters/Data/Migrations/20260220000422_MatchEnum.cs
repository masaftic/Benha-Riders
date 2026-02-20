using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenhaScooters.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""DriverMatchAttempts""
                ALTER COLUMN ""Status"" TYPE integer
                USING CASE ""Status""
                    WHEN 'Pending' THEN 1
                    WHEN 'Accepted' THEN 2
                    WHEN 'Rejected' THEN 3
                    WHEN 'Cancelled' THEN 4
                    ELSE 4
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""DriverMatchAttempts""
                ALTER COLUMN ""Status"" TYPE integer
                USING ""Status""::integer;
            ");
        }
    }
}
