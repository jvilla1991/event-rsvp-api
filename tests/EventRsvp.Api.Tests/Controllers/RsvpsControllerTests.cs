using System.Net;
using System.Net.Http.Json;
using EventRsvp.Application.DTOs;
using EventRsvp.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using EventRsvp.Infrastructure.Data;
using EventRsvp.Domain.Entities;

namespace EventRsvp.Api.Tests.Controllers;

[TestFixture]
public class RsvpsControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        var testDbName = $"TestDb_{Guid.NewGuid()}"; // Unique name per test
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var dbContextOptionsDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<EventRsvpDbContext>));
                    if (dbContextOptionsDescriptor != null)
                    {
                        services.Remove(dbContextOptionsDescriptor);
                    }

                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(EventRsvpDbContext));
                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    // Add in-memory database with unique name per test instance
                    services.AddDbContext<EventRsvpDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(testDbName);
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task CreateRsvp_WhenValidRequest_ShouldReturn201Created()
    {
        // Arrange - Create an event first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event
        {
            Title = "Test Event",
            Description = "Test Description"
        };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Yes"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RsvpResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("John Doe");
        result.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CreateRsvp_WhenInvalidRequest_ShouldReturn400BadRequest()
    {
        // Arrange - Create an event first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event
        {
            Title = "Test Event"
        };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var request = new CreateRsvpRequest
        {
            Name = string.Empty,
            Status = "Yes"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateRsvp_WhenStatusInvalid_ShouldReturn400BadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event { Title = "Test Event" };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Perhaps"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateRsvp_WhenEventNotFound_ShouldReturn404NotFound()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Yes"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/events/99999/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetRsvps_WhenRsvpsExist_ShouldReturn200Ok()
    {
        // Arrange - Create an event and RSVP
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event
        {
            Title = "Test Event"
        };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var createRequest = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Yes"
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", createRequest);
        createResponse.EnsureSuccessStatusCode(); // Ensure the create was successful

        // Act
        var response = await _client.GetAsync($"/api/events/{eventId}/rsvps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<RsvpResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task GetRsvps_WhenNoRsvps_ShouldReturnEmptyList()
    {
        // Arrange - Create an event
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event
        {
            Title = "Test Event"
        };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        // Act
        var response = await _client.GetAsync($"/api/events/{eventId}/rsvps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<RsvpResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Test]
    public async Task GetRsvps_ShouldOnlyReturnRsvpsForSpecifiedEvent()
    {
        // Arrange - Create two events
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();

        var event1 = new EventRsvp.Domain.Entities.Event { Title = "Event 1" };
        var event2 = new EventRsvp.Domain.Entities.Event { Title = "Event 2" };
        dbContext.Events.AddRange(event1, event2);
        await dbContext.SaveChangesAsync();

        // Create RSVPs for both events
        var rsvp1Request = new CreateRsvpRequest { Name = "John", Status = "Yes" };
        var rsvp2Request = new CreateRsvpRequest { Name = "Jane", Status = "Yes" };

        await _client.PostAsJsonAsync($"/api/events/{event1.Id}/rsvps", rsvp1Request);
        await _client.PostAsJsonAsync($"/api/events/{event2.Id}/rsvps", rsvp2Request);

        // Act
        var response1 = await _client.GetAsync($"/api/events/{event1.Id}/rsvps");
        var response2 = await _client.GetAsync($"/api/events/{event2.Id}/rsvps");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result1 = await response1.Content.ReadFromJsonAsync<List<RsvpResponse>>();
        var result2 = await response2.Content.ReadFromJsonAsync<List<RsvpResponse>>();

        result1!.Should().HaveCount(1);
        result1[0].Name.Should().Be("John");
        result2!.Should().HaveCount(1);
        result2[0].Name.Should().Be("Jane");
    }

    [Test]
    public async Task CreateRsvp_WhenStatusNoWithProposedTime_ShouldReturn201AndStoreProposedTime()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event { Title = "Test Event" };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var proposedTime = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc);
        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            Status = "No",
            ProposedTime = proposedTime
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RsvpResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("No");
        result.ProposedTime.Should().Be(proposedTime);
    }

    [Test]
    public async Task CreateRsvp_WhenStatusMaybeWithProposedTime_ShouldReturn201AndStoreProposedTime()
    {
        // Arrange — a Maybe responder is allowed to suggest a time
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event { Title = "Test Event" };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var proposedTime = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc);
        var request = new CreateRsvpRequest
        {
            Name = "Mona Doe",
            Status = "Maybe",
            ProposedTime = proposedTime
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RsvpResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Maybe");
        result.ProposedTime.Should().Be(proposedTime);
    }

    [Test]
    public async Task CreateRsvp_WhenStatusYesWithProposedTime_ShouldIgnoreProposedTime()
    {
        // Arrange — ProposedTime should be ignored when the answer is Yes
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event { Title = "Test Event" };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Yes",
            ProposedTime = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RsvpResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Yes");
        result.ProposedTime.Should().BeNull();
    }

    [Test]
    public async Task CreateRsvp_WhenStatusNoWithNoProposedTime_ShouldReturn201WithNullProposedTime()
    {
        // Arrange — declining with no proposed time
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventRsvpDbContext>();
        var testEvent = new EventRsvp.Domain.Entities.Event { Title = "Test Event" };
        dbContext.Events.Add(testEvent);
        await dbContext.SaveChangesAsync();
        var eventId = testEvent.Id;

        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            Status = "No",
            ProposedTime = null
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RsvpResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("No");
        result.ProposedTime.Should().BeNull();
    }
}
