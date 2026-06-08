using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantAxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVariantAxis",
                table: "AttributeDefinitions");

            migrationBuilder.CreateTable(
                name: "ProductVariantAxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantAxes_AttributeDefinitions_AttributeDefinition~",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantAxes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAxes_AttributeDefinitionId",
                table: "ProductVariantAxes",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAxes_ProductId",
                table: "ProductVariantAxes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAxes_ProductId_AttributeDefinitionId",
                table: "ProductVariantAxes",
                columns: new[] { "ProductId", "AttributeDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantAxes");

            migrationBuilder.AddColumn<bool>(
                name: "IsVariantAxis",
                table: "AttributeDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
