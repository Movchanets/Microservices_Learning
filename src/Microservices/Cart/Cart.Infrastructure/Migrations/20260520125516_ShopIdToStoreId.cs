using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShopIdToStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_Sku",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "CartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CartItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_Sku_StoreId",
                table: "CartItems",
                columns: new[] { "CartId", "Sku", "StoreId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_Sku_StoreId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CartItems");

            migrationBuilder.AddColumn<string>(
                name: "ShopId",
                table: "CartItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_Sku",
                table: "CartItems",
                columns: new[] { "CartId", "Sku" },
                unique: true);
        }
    }
}
