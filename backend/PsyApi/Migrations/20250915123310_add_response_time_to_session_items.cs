using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class add_response_time_to_session_items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeMs",
                table: "SessionItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "SessionItems");
        }
    }
}
