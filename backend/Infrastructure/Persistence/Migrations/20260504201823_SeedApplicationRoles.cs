using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedApplicationRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "security",
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("ba1e8ed9-809e-4a1a-a4b8-e3a80c87cf8f"), "ba1e8ed9-809e-4a1a-a4b8-e3a80c87cf8f", "Admin", "ADMIN" },
                    { new Guid("da637a4d-0e15-4e7d-977a-b978362e20bb"), "da637a4d-0e15-4e7d-977a-b978362e20bb", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("ba1e8ed9-809e-4a1a-a4b8-e3a80c87cf8f"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("da637a4d-0e15-4e7d-977a-b978362e20bb"));
        }
    }
}
