using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class add_response_time_column_manual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column manually with IF NOT EXISTS to be idempotent
            migrationBuilder.Sql("ALTER TABLE \"SessionItems\" ADD COLUMN IF NOT EXISTS \"ResponseTimeMs\" integer NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"SessionItems\" DROP COLUMN IF EXISTS \"ResponseTimeMs\";");
        }
    }
}
