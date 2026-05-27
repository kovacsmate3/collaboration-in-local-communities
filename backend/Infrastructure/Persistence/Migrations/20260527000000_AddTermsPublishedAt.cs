using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                schema: "config",
                table: "terms_versions",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill the seed row: it was published at seed time.
            migrationBuilder.Sql(
                """
                UPDATE config.terms_versions
                SET published_at = '2026-01-01T00:00:00+00:00'
                WHERE id = '00000000-0000-0000-0000-000000000301';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "published_at",
                schema: "config",
                table: "terms_versions");
        }
    }
}
