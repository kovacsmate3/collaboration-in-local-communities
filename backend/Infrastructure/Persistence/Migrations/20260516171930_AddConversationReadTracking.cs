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

            // Backfill: treat existing messages as already read so deployment
            // doesn't flood all users with false unread indicators.
            migrationBuilder.Sql(@"
                UPDATE data.task_conversations
                SET seeker_last_read_at = last_message_at,
                    helper_last_read_at = last_message_at
                WHERE last_message_at IS NOT NULL;
            ");
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
