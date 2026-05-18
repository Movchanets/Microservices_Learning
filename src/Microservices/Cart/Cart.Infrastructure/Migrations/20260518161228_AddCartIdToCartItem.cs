using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartIdToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ShoppingCarts_ShoppingCartBuyerId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ShoppingCartBuyerId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ShoppingCartBuyerId",
                table: "CartItems");

            migrationBuilder.AddColumn<string>(
                name: "CartId",
                table: "CartItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_Sku",
                table: "CartItems",
                columns: new[] { "CartId", "Sku" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ShoppingCarts_CartId",
                table: "CartItems",
                column: "CartId",
                principalTable: "ShoppingCarts",
                principalColumn: "BuyerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ShoppingCarts_CartId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_Sku",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CartId",
                table: "CartItems");

            migrationBuilder.AddColumn<string>(
                name: "ShoppingCartBuyerId",
                table: "CartItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShoppingCartBuyerId",
                table: "CartItems",
                column: "ShoppingCartBuyerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ShoppingCarts_ShoppingCartBuyerId",
                table: "CartItems",
                column: "ShoppingCartBuyerId",
                principalTable: "ShoppingCarts",
                principalColumn: "BuyerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
