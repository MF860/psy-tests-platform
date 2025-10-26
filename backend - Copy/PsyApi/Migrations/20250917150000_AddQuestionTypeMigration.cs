using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTypeMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // Update existing LIKERT questions to more specific types based on their text
            migrationBuilder.Sql(@"
                UPDATE Items
                SET Type = 'Frequency'
                WHERE Type = 'LIKERT' AND (
                    TextAr LIKE 'كم %' OR
                    TextAr LIKE 'كم مرة%' OR
                    TextAr LIKE 'كم ساعة%' OR
                    TextAr LIKE 'كم عدد%'
                )
            ");

            migrationBuilder.Sql(@"
                UPDATE Items
                SET Type = 'LikertAgreement'
                WHERE Type = 'LIKERT' AND (
                    TextAr LIKE 'كيف تقيم%' OR
                    TextAr LIKE 'إلى أي مدى%'
                )
            ");

            migrationBuilder.Sql(@"
                UPDATE Items
                SET Type = 'Text'
                WHERE Type = 'LIKERT' AND (
                    TextAr LIKE 'ما هي%' OR
                    TextAr LIKE 'اذكر%' OR
                    TextAr LIKE 'صف%'
                )
            ");

            // Update CorrectAnswer for Frequency questions
            migrationBuilder.Sql(@"
                UPDATE Items
                SET CorrectAnswer = 'أبدًا|نادرًا|أحيانًا|غالبًا|دائمًا'
                WHERE Type = 'Frequency' AND (CorrectAnswer IS NULL OR CorrectAnswer <> 'أبدًا|نادرًا|أحيانًا|غالبًا|دائمًا')
            ");

            // Update CorrectAnswer for LikertAgreement questions
            migrationBuilder.Sql(@"
                UPDATE Items
                SET CorrectAnswer = 'لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة'
                WHERE Type = 'LikertAgreement' AND (CorrectAnswer IS NULL OR CorrectAnswer <> 'لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة')
            ");

            // Clear CorrectAnswer for Text questions
            migrationBuilder.Sql(@"
                UPDATE Items
                SET CorrectAnswer = NULL
                WHERE Type = 'Text' AND CorrectAnswer IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert all specific types back to LIKERT
            migrationBuilder.Sql(@"
                UPDATE Items
                SET Type = 'LIKERT'
                WHERE Type IN ('Frequency', 'LikertAgreement', 'Text')
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
