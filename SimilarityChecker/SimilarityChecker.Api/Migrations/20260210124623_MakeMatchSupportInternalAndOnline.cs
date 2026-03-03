using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimilarityChecker.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeMatchSupportInternalAndOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OnlineSourceId",
                table: "Matches",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "AlgorithmVersion",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "ComparedDocumentId",
                table: "Matches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ComparedDocumentId",
                table: "Matches",
                column: "ComparedDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Documents_ComparedDocumentId",
                table: "Matches",
                column: "ComparedDocumentId",
                principalTable: "Documents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Documents_ComparedDocumentId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ComparedDocumentId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ComparedDocumentId",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "OnlineSourceId",
                table: "Matches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AlgorithmVersion",
                table: "Matches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
