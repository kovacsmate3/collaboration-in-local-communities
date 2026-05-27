using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsVersionPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_terms_versions_is_active",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                schema: "config",
                table: "terms_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "terms_versions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000301"),
                column: "published_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "ux_terms_versions_single_active",
                schema: "config",
                table: "terms_versions",
                column: "is_active",
                unique: true,
                filter: "is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_terms_versions_single_active",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.DropColumn(
                name: "published_at",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.CreateIndex(
                name: "ix_terms_versions_is_active",
                schema: "config",
                table: "terms_versions",
                column: "is_active");
        }
    }
}
