using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeddingRsvpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Rsvps",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuestCount",
                table: "Rsvps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealChoice",
                table: "Rsvps",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Rsvps",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "GuestCount",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "MealChoice",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Rsvps");
        }
    }
}
