using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSdjFieldsToItems_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dimension",
                table: "Items",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Reverse",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubDimension",
                table: "Items",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dimension",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Reverse",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SubDimension",
                table: "Items");
        }
    }
}
