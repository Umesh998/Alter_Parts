using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alter_Parts.Migrations
{
    /// <inheritdoc />
    public partial class partssss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Final_Result",
                table: "More_Fruits",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "ComparisonNotes",
                table: "More_Fruits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChecked",
                table: "More_Fruits",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComparisonNotes",
                table: "More_Fruits");

            migrationBuilder.DropColumn(
                name: "LastChecked",
                table: "More_Fruits");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "More_Fruits",
                newName: "Final_Result");
        }
    }
}
