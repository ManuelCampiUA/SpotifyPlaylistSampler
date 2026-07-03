using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDataIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Playlists_SpotifyId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_CanvasNodes_ReferenceId",
                table: "CanvasNodes");

            migrationBuilder.AddColumn<string>(
                name: "UserSpotifyId",
                table: "Playlists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserSpotifyId",
                table: "CanvasNodes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_SpotifyId_UserSpotifyId",
                table: "Playlists",
                columns: new[] { "SpotifyId", "UserSpotifyId" });

            migrationBuilder.CreateIndex(
                name: "IX_CanvasNodes_ReferenceId_UserSpotifyId",
                table: "CanvasNodes",
                columns: new[] { "ReferenceId", "UserSpotifyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Playlists_SpotifyId_UserSpotifyId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_CanvasNodes_ReferenceId_UserSpotifyId",
                table: "CanvasNodes");

            migrationBuilder.DropColumn(
                name: "UserSpotifyId",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "UserSpotifyId",
                table: "CanvasNodes");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_SpotifyId",
                table: "Playlists",
                column: "SpotifyId");

            migrationBuilder.CreateIndex(
                name: "IX_CanvasNodes_ReferenceId",
                table: "CanvasNodes",
                column: "ReferenceId",
                unique: true);
        }
    }
}
