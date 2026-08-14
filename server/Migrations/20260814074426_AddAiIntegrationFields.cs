using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAiIntegrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentKnowledgeBase",
                table: "Posts",
                type: "mediumtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GraphJson",
                table: "Posts",
                type: "mediumtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentThreadId",
                table: "NoteHighlights",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentKnowledgeBase",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "GraphJson",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AgentThreadId",
                table: "NoteHighlights");
        }
    }
}
