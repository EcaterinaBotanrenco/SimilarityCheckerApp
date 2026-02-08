using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimilarityChecker.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnlineSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchQueries_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnlineSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchType = table.Column<int>(type: "int", nullable: false),
                    DocStart = table.Column<int>(type: "int", nullable: false),
                    DocEnd = table.Column<int>(type: "int", nullable: false),
                    SourceStart = table.Column<int>(type: "int", nullable: false),
                    SourceEnd = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Matches_OnlineSources_OnlineSourceId",
                        column: x => x.OnlineSourceId,
                        principalTable: "OnlineSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SearchQueryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Snippet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchResults_SearchQueries_SearchQueryId",
                        column: x => x.SearchQueryId,
                        principalTable: "SearchQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Sha256",
                table: "Documents",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DocumentId",
                table: "Matches",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_OnlineSourceId",
                table: "Matches",
                column: "OnlineSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineSources_Url",
                table: "OnlineSources",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_DocumentId",
                table: "Reports",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_DocumentId",
                table: "SearchQueries",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResults_SearchQueryId",
                table: "SearchResults",
                column: "SearchQueryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "SearchResults");

            migrationBuilder.DropTable(
                name: "OnlineSources");

            migrationBuilder.DropTable(
                name: "SearchQueries");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
