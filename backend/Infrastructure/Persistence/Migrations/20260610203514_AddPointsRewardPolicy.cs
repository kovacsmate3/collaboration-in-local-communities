using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsRewardPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_points_ledger_entry_type",
                schema: "data",
                table: "points_ledger");

            migrationBuilder.AddColumn<decimal>(
                name: "points_weight",
                schema: "config",
                table: "categories",
                type: "numeric(4,2)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                column: "points_weight",
                value: 1.5m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"),
                column: "points_weight",
                value: 1.3m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"),
                column: "points_weight",
                value: 1.4m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                column: "points_weight",
                value: 1.0m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                column: "points_weight",
                value: 1.1m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                column: "points_weight",
                value: 1.2m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"),
                column: "points_weight",
                value: 1.0m);

            migrationBuilder.UpdateData(
                schema: "config",
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"),
                column: "points_weight",
                value: 1.0m);

            migrationBuilder.CreateIndex(
                name: "ux_points_ledger_task_review_bonus_once",
                schema: "data",
                table: "points_ledger",
                columns: new[] { "task_id", "profile_id", "entry_type" },
                unique: true,
                filter: "entry_type = 'ReviewQualityBonus'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_points_ledger_entry_type",
                schema: "data",
                table: "points_ledger",
                sql: "entry_type IN ('TaskCompletedReward', 'ReviewQualityBonus', 'ManualAdjustment', 'Redemption')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_points_weight_positive",
                schema: "config",
                table: "categories",
                sql: "points_weight > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_points_ledger_task_review_bonus_once",
                schema: "data",
                table: "points_ledger");

            migrationBuilder.DropCheckConstraint(
                name: "ck_points_ledger_entry_type",
                schema: "data",
                table: "points_ledger");

            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_points_weight_positive",
                schema: "config",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "points_weight",
                schema: "config",
                table: "categories");

            migrationBuilder.AddCheckConstraint(
                name: "ck_points_ledger_entry_type",
                schema: "data",
                table: "points_ledger",
                sql: "entry_type IN ('TaskCompletedReward', 'ManualAdjustment', 'Redemption')");
        }
    }
}
