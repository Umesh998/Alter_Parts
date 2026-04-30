using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alter_Parts.Migrations
{
    /// <inheritdoc />
    public partial class sssss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Final_Result",
                table: "More_Fruits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Final_Result",
                table: "More_Fruits");
        }
    }
}
