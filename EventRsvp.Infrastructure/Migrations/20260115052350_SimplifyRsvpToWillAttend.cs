using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRsvp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyRsvpToWillAttend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add WillAttend column only if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Rsvps' AND column_name = 'WillAttend'
                    ) THEN
                        ALTER TABLE ""Rsvps"" ADD COLUMN ""WillAttend"" boolean NOT NULL DEFAULT TRUE;
                    END IF;
                END $$;
            ");

            // Drop old columns only if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Rsvps' AND column_name = 'BringingDish'
                    ) THEN
                        ALTER TABLE ""Rsvps"" DROP COLUMN ""BringingDish"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Rsvps' AND column_name = 'Dishes'
                    ) THEN
                        ALTER TABLE ""Rsvps"" DROP COLUMN ""Dishes"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Rsvps' AND column_name = 'WhiteElephant'
                    ) THEN
                        ALTER TABLE ""Rsvps"" DROP COLUMN ""WhiteElephant"";
                    END IF;
                END $$;
            ");

            // Add EventId only if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Rsvps' AND column_name = 'EventId'
                    ) THEN
                        ALTER TABLE ""Rsvps"" ADD COLUMN ""EventId"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");

            // Create index only if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'Rsvps' AND indexname = 'IX_Rsvps_EventId'
                    ) THEN
                        CREATE INDEX ""IX_Rsvps_EventId"" ON ""Rsvps"" (""EventId"");
                    END IF;
                END $$;
            ");

            // Add foreign key only if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE table_name = 'Rsvps' AND constraint_name = 'FK_Rsvps_Events_EventId'
                    ) THEN
                        ALTER TABLE ""Rsvps"" 
                        ADD CONSTRAINT ""FK_Rsvps_Events_EventId"" 
                        FOREIGN KEY (""EventId"") 
                        REFERENCES ""Events"" (""Id"") 
                        ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop EventId foreign key and column
            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_Events_EventId",
                table: "Rsvps");

            migrationBuilder.DropIndex(
                name: "IX_Rsvps_EventId",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Rsvps");

            // Restore old columns
            migrationBuilder.AddColumn<bool>(
                name: "BringingDish",
                table: "Rsvps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Dishes",
                table: "Rsvps",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "WhiteElephant",
                table: "Rsvps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Drop WillAttend
            migrationBuilder.DropColumn(
                name: "WillAttend",
                table: "Rsvps");
        }
    }
}
