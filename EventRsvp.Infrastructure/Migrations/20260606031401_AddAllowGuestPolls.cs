using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowGuestPolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowGuestPolls",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowGuestPolls",
                table: "Events");
        }
    }
}
