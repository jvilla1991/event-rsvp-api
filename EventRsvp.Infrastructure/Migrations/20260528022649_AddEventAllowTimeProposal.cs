using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAllowTimeProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowTimeProposal",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowTimeProposal",
                table: "Events");
        }
    }
}
