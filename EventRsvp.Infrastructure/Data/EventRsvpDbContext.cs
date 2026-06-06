using EventRsvp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace EventRsvp.Infrastructure.Data;

public class EventRsvpDbContext : DbContext
{
    public EventRsvpDbContext(DbContextOptions<EventRsvpDbContext> options) : base(options)
    {
    }

    public DbSet<Rsvp> Rsvps { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollVote> PollVotes { get; set; }
    public DbSet<Invite> Invites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rsvp>(entity =>
        {
            entity.ToTable("Rsvps");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EventId)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.WillAttend)
                .IsRequired();

            entity.Property(e => e.ProposedTime)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Configure foreign key relationship to Event
            entity.HasOne<Event>()
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Address)
                .HasMaxLength(500);

            entity.Property(e => e.EventDateTime)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.AllowTimeProposal)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.AllowGuestPolls)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Poll>(entity =>
        {
            entity.ToTable("Polls");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EventId)
                .IsRequired();

            entity.Property(e => e.Question)
                .IsRequired();

            // Configure JSON storage for Options array
            var optionsProperty = entity.Property(e => e.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>())
                .HasColumnType("jsonb");

            // Configure ValueComparer for proper change tracking
            optionsProperty.Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

            entity.Property(e => e.AllowMultiple)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasColumnType("timestamp with time zone");

            // Configure foreign key relationship to Event with cascade delete
            entity.HasOne<Event>()
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PollVote>(entity =>
        {
            entity.ToTable("PollVotes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.PollId)
                .IsRequired();

            entity.Property(e => e.VoterName)
                .IsRequired()
                .HasMaxLength(200);

            // Configure JSON storage for SelectedOptions array
            var selectedOptionsProperty = entity.Property(e => e.SelectedOptions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!) ?? new List<int>())
                .HasColumnType("jsonb");

            // Configure ValueComparer for proper change tracking
            selectedOptionsProperty.Metadata.SetValueComparer(new ValueComparer<List<int>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Configure foreign key relationship to Poll with cascade delete
            entity.HasOne<Poll>()
                .WithMany()
                .HasForeignKey(e => e.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invite>(entity =>
        {
            entity.ToTable("Invites");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EventId)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(e => e.Token)
                .IsUnique();

            // Status is NOT configured with HasDefaultValue() intentionally.
            // Using HasDefaultValue(0) sets ValueGeneratedOnAdd on the property, which
            // causes EF Core to treat 0 as a sentinel and exclude Status from UPDATE
            // statements when the value is 0. The entity initialiser already defaults
            // Status to NotOpened in C#; no DB-side default is needed.
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.ViewedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Configure foreign key relationship to Event with cascade delete
            entity.HasOne<Event>()
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

