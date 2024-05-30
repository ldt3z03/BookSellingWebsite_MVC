using Microsoft.EntityFrameworkCore.Migrations;

namespace BookSelling.DataAccess.Migrations
{
    public partial class AddOrderDetailsToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderDetails",
                table: "Products",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderDetails",
                table: "Products");
        }
    }
}
