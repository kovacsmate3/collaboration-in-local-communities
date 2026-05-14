using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                schema: "config",
                table: "skills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "config",
                table: "skills",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000206"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000207"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000208"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000209"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000210"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000211"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000212"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000213"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000214"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000215"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000216"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000217"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000218"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000219"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.UpdateData(
                schema: "config",
                table: "skills",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000220"),
                columns: new[] { "approved_at", "status" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved" });

            migrationBuilder.CreateIndex(
                name: "ix_skills_status",
                schema: "config",
                table: "skills",
                column: "status");

            migrationBuilder.AddCheckConstraint(
                name: "ck_skills_status",
                schema: "config",
                table: "skills",
                sql: "status IN ('Pending', 'Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skills_status",
                schema: "config",
                table: "skills");

            migrationBuilder.DropCheckConstraint(
                name: "ck_skills_status",
                schema: "config",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "approved_at",
                schema: "config",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "config",
                table: "skills");
        }
    }
}
