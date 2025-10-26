using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class add_scoring_params_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RawCorrect",
                table: "SessionItems",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeMs",
                table: "SessionItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompositeScoresJson",
                table: "Results",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DimensionScoresJson",
                table: "Results",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoringModelVersion",
                table: "Results",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    A = table.Column<double>(type: "double precision", nullable: true),
                    B = table.Column<double>(type: "double precision", nullable: true),
                    C = table.Column<double>(type: "double precision", nullable: true),
                    ThresholdsJson = table.Column<string>(type: "text", nullable: true),
                    PcmStepsJson = table.Column<string>(type: "text", nullable: true),
                    TimeAlpha = table.Column<double>(type: "double precision", nullable: true),
                    TimeBeta = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemParameters_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemParameters_ItemId",
                table: "ItemParameters",
                column: "ItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemParameters");

            migrationBuilder.DropColumn(
                name: "RawCorrect",
                table: "SessionItems");

            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "SessionItems");

            migrationBuilder.DropColumn(
                name: "CompositeScoresJson",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "DimensionScoresJson",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "ScoringModelVersion",
                table: "Results");
        }
    }
}
