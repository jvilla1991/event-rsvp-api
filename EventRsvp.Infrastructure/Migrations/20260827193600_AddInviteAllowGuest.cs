using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteAllowGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowGuest",
                table: "Invites",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowGuest",
                table: "Invites");
        }
    }
}
