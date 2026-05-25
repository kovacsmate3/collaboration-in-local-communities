using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TermsVersionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content",
                schema: "config",
                table: "terms_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "major_version",
                schema: "config",
                table: "terms_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "minor_version",
                schema: "config",
                table: "terms_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "patch_version",
                schema: "config",
                table: "terms_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "config",
                table: "terms_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.DropIndex(
                name: "ux_terms_versions_version",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.CreateIndex(
                name: "ux_terms_versions_version_triple",
                schema: "config",
                table: "terms_versions",
                columns: new[] { "major_version", "minor_version", "patch_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terms_versions_major_minor",
                schema: "config",
                table: "terms_versions",
                columns: new[] { "major_version", "minor_version" });

            // Populate version components for the existing seed row, update version string, and add content.
            migrationBuilder.Sql(
                """
                UPDATE config.terms_versions
                SET
                    major_version = 0,
                    minor_version = 1,
                    patch_version = 0,
                    version = '0.1.0',
                    content = '<h2>1. About 2gather</h2><p>2gather is a platform that connects neighbours who need help with those willing to offer it. By creating an account you agree to use the service in good faith and in accordance with your local laws.</p><h2>2. Your account</h2><p>You are responsible for keeping your credentials secure and for all activity that occurs under your account. Notify us immediately if you suspect unauthorised access.</p><h2>3. Acceptable use</h2><p>You may not use 2gather to post illegal tasks, harass other users, misrepresent your identity, or scrape the platform without written permission. We reserve the right to suspend accounts that violate these rules.</p><h2>4. Payments &amp; barter</h2><p>2gather facilitates agreements between users but is not a party to them. Any payment or exchange is solely between the Seeker and the Helper. We are not liable for disputes arising from tasks.</p><h2>5. Privacy</h2><p>We collect only the information needed to operate the service. We do not sell your data. A full privacy policy will be published before the public launch.</p><h2>6. Limitation of liability</h2><p>2gather is provided &#x201C;as is&#x201D; during this early phase. We make no warranties about uptime or fitness for a particular purpose. Our liability is limited to the maximum extent permitted by applicable law.</p><h2>7. Changes to these terms</h2><p>We may update these terms as the product evolves. Continued use of the platform after changes are posted constitutes acceptance of the revised terms.</p>'
                WHERE id = '00000000-0000-0000-0000-000000000301';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_terms_versions_version_triple",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.DropIndex(
                name: "ix_terms_versions_major_minor",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.Sql(
                """
                UPDATE config.terms_versions
                SET version = '1.0', is_active = true
                WHERE id = '00000000-0000-0000-0000-000000000301';
                """);

            migrationBuilder.DropColumn(
                name: "content",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.DropColumn(
                name: "major_version",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.DropColumn(
                name: "minor_version",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.DropColumn(
                name: "patch_version",
                schema: "config",
                table: "terms_versions");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "config",
                table: "terms_versions",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ux_terms_versions_version",
                schema: "config",
                table: "terms_versions",
                column: "version",
                unique: true);
        }
    }
}
