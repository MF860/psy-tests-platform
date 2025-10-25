using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSdjFieldsToItems : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "SubDimension",
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
                
            // Add indexes for performance (SDJ queries will filter by Dimension/SubDimension)
            migrationBuilder.CreateIndex(
                name: "IX_Items_Dimension",
                table: "Items",
                column: "Dimension");
                
            migrationBuilder.CreateIndex(
                name: "IX_Items_SubDimension",
                table: "Items",
                column: "SubDimension");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Dimension",
                table: "Items");
                
            migrationBuilder.DropIndex(
                name: "IX_Items_SubDimension",
                table: "Items");
                
            migrationBuilder.DropColumn(
                name: "Dimension",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SubDimension",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Reverse",
                table: "Items");
        }
    }
}
