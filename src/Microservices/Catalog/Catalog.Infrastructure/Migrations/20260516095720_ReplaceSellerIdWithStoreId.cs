using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSellerIdWithStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "Products",
                newName: "StoreId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SellerId",
                table: "Products",
                newName: "IX_Products_StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StoreId",
                table: "Products",
                newName: "SellerId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_StoreId",
                table: "Products",
                newName: "IX_Products_SellerId");
        }
    }
}
