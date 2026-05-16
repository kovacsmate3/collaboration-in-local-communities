using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationReadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "helper_last_read_at",
                schema: "data",
                table: "task_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "seeker_last_read_at",
                schema: "data",
                table: "task_conversations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "helper_last_read_at",
                schema: "data",
                table: "task_conversations");

            migrationBuilder.DropColumn(
                name: "seeker_last_read_at",
                schema: "data",
                table: "task_conversations");
        }
    }
}
