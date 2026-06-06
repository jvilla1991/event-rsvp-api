using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRsvpStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert the boolean WillAttend column into the RsvpStatus enum column,
            // preserving existing answers: true => Yes (1), false => No (0).
            // (No existing row can be Maybe (2); that value only arrives via new RSVPs.)
            migrationBuilder.Sql(@"ALTER TABLE ""Rsvps"" ADD COLUMN ""Status"" integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql(@"UPDATE ""Rsvps"" SET ""Status"" = CASE WHEN ""WillAttend"" THEN 1 ELSE 0 END;");
            migrationBuilder.Sql(@"ALTER TABLE ""Rsvps"" DROP COLUMN ""WillAttend"";");
            // The model defines no DB-side default for Status; drop the temporary one used for backfill.
            migrationBuilder.Sql(@"ALTER TABLE ""Rsvps"" ALTER COLUMN ""Status"" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the conversion: only a definite Yes (1) maps back to true;
            // No (0) and Maybe (2) both collapse to false.
            migrationBuilder.Sql(@"ALTER TABLE ""Rsvps"" ADD COLUMN ""WillAttend"" boolean NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql(@"UPDATE ""Rsvps"" SET ""WillAttend"" = (""Status"" = 1);");
            migrationBuilder.Sql(@"ALTER TABLE ""Rsvps"" DROP COLUMN ""Status"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Rsvps"" ALTER COLUMN ""WillAttend"" DROP DEFAULT;");
        }
    }
}
