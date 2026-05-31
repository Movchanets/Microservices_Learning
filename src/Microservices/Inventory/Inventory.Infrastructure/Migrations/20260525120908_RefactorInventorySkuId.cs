using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInventorySkuId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "InventoryItems",
                newName: "SkuCode");

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SkuId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SkuCode",
                table: "InventoryItems",
                column: "SkuCode");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems",
                column: "SkuId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SkuCode",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SkuId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "SkuCode",
                table: "InventoryItems",
                newName: "Sku");
        }
    }
}
