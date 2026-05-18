using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSellerIdAndAddXminConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "ShoppingCarts");

            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "CartItems",
                newName: "ShopId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ShoppingCarts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShoppingCarts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "CartItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ShoppingCarts");

            migrationBuilder.RenameColumn(
                name: "ShopId",
                table: "CartItems",
                newName: "SellerId");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "ShoppingCarts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "CartItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
