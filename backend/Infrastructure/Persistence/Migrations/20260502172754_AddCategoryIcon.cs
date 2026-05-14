using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icon",
                schema: "config",
                table: "categories",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "MoreHorizontalCircle01Icon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                column: "icon",
                value: "DeliveryTruck01Icon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"),
                column: "icon",
                value: "Mortarboard02Icon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"),
                column: "icon",
                value: "Wrench01Icon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                column: "icon",
                value: "ShoppingBag03Icon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                column: "icon",
                value: "Bone01Icon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                column: "icon",
                value: "SparklesIcon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"),
                column: "icon",
                value: "RunningShoesIcon");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"),
                column: "icon",
                value: "MoreHorizontalCircle01Icon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icon",
                schema: "config",
                table: "categories");
        }
    }
}
