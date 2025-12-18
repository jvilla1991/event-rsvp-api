using EventRsvp.Domain.Entities;
using EventRsvp.Infrastructure.Data;
using EventRsvp.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EventRsvp.Infrastructure.Tests.Repositories;

[TestFixture]
public class RsvpRepositoryTests
{
    private EventRsvpDbContext _context = null!;
    private RsvpRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<EventRsvpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventRsvpDbContext(options);
        _repository = new RsvpRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_WhenValidRsvp_ShouldAddToDatabase()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "Pasta Salad" },
            WhiteElephant = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(rsvp);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        var savedRsvp = await _context.Rsvps.FindAsync(result.Id);
        savedRsvp.Should().NotBeNull();
        savedRsvp!.Name.Should().Be("John Doe");
    }

    [Test]
    public async Task GetAllAsync_WhenRsvpsExist_ShouldReturnAllRsvps()
    {
        // Arrange
        var rsvp1 = new Rsvp { Name = "John", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var rsvp2 = new Rsvp { Name = "Jane", CreatedAt = DateTime.UtcNow.AddHours(-1) };
        
        _context.Rsvps.AddRange(rsvp1, rsvp2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Jane"); // Newest first
        result[1].Name.Should().Be("John");
    }

    [Test]
    public async Task GetAllAsync_WhenNoRsvps_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }
}

