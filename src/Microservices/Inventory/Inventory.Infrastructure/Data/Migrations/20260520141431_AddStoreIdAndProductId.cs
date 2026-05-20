using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreIdAndProductId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "InventoryItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "InventoryItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "InventoryItems");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems",
                column: "Sku",
                unique: true);
        }
    }
}
