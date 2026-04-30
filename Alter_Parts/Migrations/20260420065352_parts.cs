using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alter_Parts.Migrations
{
    /// <inheritdoc />
    public partial class parts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "More_Fruits",
                columns: table => new
                {
                    Part_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Original_Part_Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Alter_Part_Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Final_Result = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_More_Fruits", x => x.Part_Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "More_Fruits");
        }
    }
}
