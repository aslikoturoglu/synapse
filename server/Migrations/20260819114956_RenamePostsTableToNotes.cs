using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostsTableToNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrainMapKeywords_Posts_PostId",
                table: "BrainMapKeywords");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_NoteFiles_Posts_PostId",
                table: "NoteFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_NoteHighlights_Posts_PostId",
                table: "NoteHighlights");

            migrationBuilder.DropForeignKey(
                name: "FK_NotePages_Posts_PostId",
                table: "NotePages");

            migrationBuilder.DropForeignKey(
                name: "FK_PostFavorites_Posts_FavoritedPostsId",
                table: "PostFavorites");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Posts_LikedPostsId",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostReposts_Posts_RepostedPostsId",
                table: "PostReposts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Users_AuthorId",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Posts",
                table: "Posts");

            migrationBuilder.RenameTable(
                name: "Posts",
                newName: "Notes");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_GroupId",
                table: "Notes",
                newName: "IX_Notes_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_AuthorId",
                table: "Notes",
                newName: "IX_Notes_AuthorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notes",
                table: "Notes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BrainMapKeywords_Notes_PostId",
                table: "BrainMapKeywords",
                column: "PostId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Notes_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NoteFiles_Notes_PostId",
                table: "NoteFiles",
                column: "PostId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NoteHighlights_Notes_PostId",
                table: "NoteHighlights",
                column: "PostId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotePages_Notes_PostId",
                table: "NotePages",
                column: "PostId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Groups_GroupId",
                table: "Notes",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_AuthorId",
                table: "Notes",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostFavorites_Notes_FavoritedPostsId",
                table: "PostFavorites",
                column: "FavoritedPostsId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Notes_LikedPostsId",
                table: "PostLikes",
                column: "LikedPostsId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostReposts_Notes_RepostedPostsId",
                table: "PostReposts",
                column: "RepostedPostsId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrainMapKeywords_Notes_PostId",
                table: "BrainMapKeywords");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Notes_PostId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_NoteFiles_Notes_PostId",
                table: "NoteFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_NoteHighlights_Notes_PostId",
                table: "NoteHighlights");

            migrationBuilder.DropForeignKey(
                name: "FK_NotePages_Notes_PostId",
                table: "NotePages");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Groups_GroupId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_AuthorId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostFavorites_Notes_FavoritedPostsId",
                table: "PostFavorites");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Notes_LikedPostsId",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostReposts_Notes_RepostedPostsId",
                table: "PostReposts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notes",
                table: "Notes");

            migrationBuilder.RenameTable(
                name: "Notes",
                newName: "Posts");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_GroupId",
                table: "Posts",
                newName: "IX_Posts_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_AuthorId",
                table: "Posts",
                newName: "IX_Posts_AuthorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Posts",
                table: "Posts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BrainMapKeywords_Posts_PostId",
                table: "BrainMapKeywords",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NoteFiles_Posts_PostId",
                table: "NoteFiles",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NoteHighlights_Posts_PostId",
                table: "NoteHighlights",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotePages_Posts_PostId",
                table: "NotePages",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostFavorites_Posts_FavoritedPostsId",
                table: "PostFavorites",
                column: "FavoritedPostsId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Posts_LikedPostsId",
                table: "PostLikes",
                column: "LikedPostsId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostReposts_Posts_RepostedPostsId",
                table: "PostReposts",
                column: "RepostedPostsId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Users_AuthorId",
                table: "Posts",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
