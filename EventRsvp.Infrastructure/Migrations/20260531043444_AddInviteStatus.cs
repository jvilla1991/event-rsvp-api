using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Invites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: any invite that was already opened before this migration
            // (ViewedAt is set) should be Opened (1), not NotOpened (0).
            migrationBuilder.Sql(
                "UPDATE \"Invites\" SET \"Status\" = 1 WHERE \"ViewedAt\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invites");
        }
    }
}
