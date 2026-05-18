using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCartPkToGuidId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop existing FK
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ShoppingCarts_CartId",
                table: "CartItems");

            // 2. Drop old PK (BuyerId)
            migrationBuilder.DropPrimaryKey(
                name: "PK_ShoppingCarts",
                table: "ShoppingCarts");

            // 3. Add Id column with auto-generated UUID for existing rows
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "ShoppingCarts",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            // 4. Add temp column to hold the new Guid FK
            migrationBuilder.AddColumn<Guid>(
                name: "CartId_new",
                table: "CartItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // 5. Populate temp FK by joining on BuyerId
            migrationBuilder.Sql("""
                UPDATE "CartItems" ci
                SET "CartId_new" = sc."Id"
                FROM "ShoppingCarts" sc
                WHERE ci."CartId" = sc."BuyerId"
                """);

            // 6. Drop old CartId (string), rename new one
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_Sku",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CartId",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "CartId_new",
                table: "CartItems",
                newName: "CartId");

            // 7. New PK on Id
            migrationBuilder.AddPrimaryKey(
                name: "PK_ShoppingCarts",
                table: "ShoppingCarts",
                column: "Id");

            // 8. Unique index on BuyerId
            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_BuyerId",
                table: "ShoppingCarts",
                column: "BuyerId",
                unique: true);

            // 9. Recreate CartItems indexes and FK
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
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ShoppingCarts_CartId",
                table: "CartItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShoppingCarts",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_BuyerId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ShoppingCarts");

            migrationBuilder.AlterColumn<string>(
                name: "CartId",
                table: "CartItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShoppingCarts",
                table: "ShoppingCarts",
                column: "BuyerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ShoppingCarts_CartId",
                table: "CartItems",
                column: "CartId",
                principalTable: "ShoppingCarts",
                principalColumn: "BuyerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
