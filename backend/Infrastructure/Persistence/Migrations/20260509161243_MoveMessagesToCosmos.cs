using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveMessagesToCosmos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "messages",
                schema: "data");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_message_at",
                schema: "data",
                table: "task_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_message_content",
                schema: "data",
                table: "task_conversations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_message_at",
                schema: "data",
                table: "task_conversations");

            migrationBuilder.DropColumn(
                name: "last_message_content",
                schema: "data",
                table: "task_conversations");

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_profiles_sender_profile_id",
                        column: x => x.sender_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_messages_task_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "data",
                        principalTable: "task_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_sent_at",
                schema: "data",
                table: "messages",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_profile_id",
                schema: "data",
                table: "messages",
                column: "sender_profile_id");
        }
    }
}
