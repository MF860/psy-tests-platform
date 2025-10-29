using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class SDJ_V2_SevenPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PatternId",
                table: "Items",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternKey",
                table: "Items",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternNameAr",
                table: "Items",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubId",
                table: "Items",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubKey",
                table: "Items",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubNameAr",
                table: "Items",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatternId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PatternKey",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PatternNameAr",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SubId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SubKey",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SubNameAr",
                table: "Items");
        }
    }
}
