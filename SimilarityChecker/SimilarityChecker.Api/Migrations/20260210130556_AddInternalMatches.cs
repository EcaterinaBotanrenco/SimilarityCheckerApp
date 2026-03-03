using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimilarityChecker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternalMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComparedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalMatches_Documents_ComparedDocumentId",
                        column: x => x.ComparedDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InternalMatches_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternalMatches_ComparedDocumentId",
                table: "InternalMatches",
                column: "ComparedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalMatches_DocumentId",
                table: "InternalMatches",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternalMatches");
        }
    }
}
