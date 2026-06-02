using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryEntrySkuId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SkuId",
                table: "GalleryEntries",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkuId",
                table: "GalleryEntries");
        }
    }
}
