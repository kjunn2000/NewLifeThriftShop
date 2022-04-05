using Microsoft.EntityFrameworkCore.Migrations;

namespace NewLifeThriftShop.Migrations.NewLifeThriftShop_New
{
    public partial class addImgExt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImgExt",
                table: "Product",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImgExt",
                table: "Product");
        }
    }
}
