using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentChangeCount",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BrainMapKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "longtext", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrainMapKeywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrainMapKeywords_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NoteHighlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    SelectedText = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteHighlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteHighlights_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteHighlights_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotePages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Heading = table.Column<string>(type: "longtext", nullable: true),
                    Body = table.Column<string>(type: "mediumtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotePages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotePages_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AiChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    HighlightId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Question = table.Column<string>(type: "longtext", nullable: false),
                    Answer = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiChatMessages_NoteHighlights_HighlightId",
                        column: x => x.HighlightId,
                        principalTable: "NoteHighlights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatMessages_HighlightId",
                table: "AiChatMessages",
                column: "HighlightId");

            migrationBuilder.CreateIndex(
                name: "IX_BrainMapKeywords_PostId",
                table: "BrainMapKeywords",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteHighlights_PostId",
                table: "NoteHighlights",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteHighlights_UserId",
                table: "NoteHighlights",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotePages_PostId",
                table: "NotePages",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiChatMessages");

            migrationBuilder.DropTable(
                name: "BrainMapKeywords");

            migrationBuilder.DropTable(
                name: "NotePages");

            migrationBuilder.DropTable(
                name: "NoteHighlights");

            migrationBuilder.DropColumn(
                name: "DocumentChangeCount",
                table: "Posts");
        }
    }
}
