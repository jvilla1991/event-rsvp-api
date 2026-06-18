using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAttendingLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendingLimit",
                table: "Events",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendingLimit",
                table: "Events");
        }
    }
}
