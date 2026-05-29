using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCanvasTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanvasNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NodeType = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceId = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    PositionX = table.Column<double>(type: "REAL", nullable: false),
                    PositionY = table.Column<double>(type: "REAL", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPlaylistId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanvasNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanvasEdges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceNodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetNodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EdgeType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanvasEdges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanvasEdges_CanvasNodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "CanvasNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanvasEdges_CanvasNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "CanvasNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanvasEdges_SourceNodeId",
                table: "CanvasEdges",
                column: "SourceNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CanvasEdges_TargetNodeId",
                table: "CanvasEdges",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CanvasNodes_ReferenceId",
                table: "CanvasNodes",
                column: "ReferenceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanvasEdges");

            migrationBuilder.DropTable(
                name: "CanvasNodes");
        }
    }
}
