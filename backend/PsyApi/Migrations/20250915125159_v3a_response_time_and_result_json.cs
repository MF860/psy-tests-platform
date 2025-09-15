using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class v3a_response_time_and_result_json : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure SessionItems.ResponseTimeMs exists
            migrationBuilder.Sql("ALTER TABLE \"SessionItems\" ADD COLUMN IF NOT EXISTS \"ResponseTimeMs\" integer NULL;");

            // Ensure Results extra JSON fields exist
            migrationBuilder.Sql("ALTER TABLE \"Results\" ADD COLUMN IF NOT EXISTS \"DimensionScoresJson\" text NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Results\" ADD COLUMN IF NOT EXISTS \"CompositeScoresJson\" text NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Results\" ADD COLUMN IF NOT EXISTS \"ScoringModelVersion\" character varying(32) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"SessionItems\" DROP COLUMN IF EXISTS \"ResponseTimeMs\";");
            migrationBuilder.Sql("ALTER TABLE \"Results\" DROP COLUMN IF EXISTS \"DimensionScoresJson\";");
            migrationBuilder.Sql("ALTER TABLE \"Results\" DROP COLUMN IF EXISTS \"CompositeScoresJson\";");
            migrationBuilder.Sql("ALTER TABLE \"Results\" DROP COLUMN IF EXISTS \"ScoringModelVersion\";");
        }
    }
}
