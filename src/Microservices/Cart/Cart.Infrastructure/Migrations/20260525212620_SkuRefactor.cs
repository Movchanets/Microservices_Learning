using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SkuRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_Sku",
                table: "ProductPrices");

            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "ProductPrices",
                newName: "SkuCode");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ProductPrices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SkuId",
                table: "ProductPrices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SkuCode",
                table: "CartItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SkuId",
                table: "CartItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductId",
                table: "ProductPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_SkuId",
                table: "ProductPrices",
                column: "SkuId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_ProductId",
                table: "ProductPrices");

            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_SkuId",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "SkuId",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "SkuCode",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "SkuId",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "SkuCode",
                table: "ProductPrices",
                newName: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_Sku",
                table: "ProductPrices",
                column: "Sku",
                unique: true);
        }
    }
}
