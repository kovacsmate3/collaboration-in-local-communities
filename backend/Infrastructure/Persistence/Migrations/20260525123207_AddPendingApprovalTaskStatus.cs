using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingApprovalTaskStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tasks_status",
                schema: "data",
                table: "tasks");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tasks_status",
                schema: "data",
                table: "tasks",
                sql: "status IN ('Open', 'InProgress', 'PendingApproval', 'Completed', 'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tasks_status",
                schema: "data",
                table: "tasks");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tasks_status",
                schema: "data",
                table: "tasks",
                sql: "status IN ('Open', 'InProgress', 'Completed', 'Cancelled')");
        }
    }
}
