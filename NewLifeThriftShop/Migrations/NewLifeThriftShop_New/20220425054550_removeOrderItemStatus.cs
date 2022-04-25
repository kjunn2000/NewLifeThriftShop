using Microsoft.EntityFrameworkCore.Migrations;

namespace NewLifeThriftShop.Migrations.NewLifeThriftShop_New
{
    public partial class removeOrderItemStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderItem");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "Product",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OrderItem",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
