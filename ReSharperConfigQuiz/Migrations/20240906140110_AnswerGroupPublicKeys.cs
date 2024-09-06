using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReSharperConfigQuiz.Migrations
{
    /// <inheritdoc />
    public partial class AnswerGroupPublicKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "AnswerGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AnswerGroups_PublicId",
                table: "AnswerGroups",
                column: "PublicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_AnswerGroups_PublicId",
                table: "AnswerGroups");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "AnswerGroups");
        }
    }
}
