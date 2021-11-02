using Microsoft.EntityFrameworkCore.Migrations;

namespace DiplomaWebApp.Migrations
{
    public partial class And_Random_Name : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RandomNameInServer",
                table: "MusicFiles",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RandomNameInServer",
                table: "MusicFiles");
        }
    }
}
