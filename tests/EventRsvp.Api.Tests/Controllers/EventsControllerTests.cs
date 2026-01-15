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

namespace EventRsvp.Api.Tests.Controllers;

[TestFixture]
public class EventsControllerTests
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
    public async Task CreateEvent_WhenValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            Address = "123 Test Street"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<EventResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Event");
        result.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetEvent_WhenEventExists_ShouldReturn200Ok()
    {
        // Arrange - Create a test event first
        var createRequest = new CreateEventRequest
        {
            Title = "Test Event",
            EventDateTime = DateTime.UtcNow.AddDays(7)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act
        var response = await _client.GetAsync($"/api/events/{createdEvent!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EventResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdEvent.Id);
        result.Title.Should().Be("Test Event");
    }

    [Test]
    public async Task GetEvent_WhenEventDoesNotExist_ShouldReturn404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/events/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteEvent_WhenEventExists_ShouldReturn204NoContent()
    {
        // Arrange - Create a test event first
        var createRequest = new CreateEventRequest
        {
            Title = "Event to Delete",
            EventDateTime = DateTime.UtcNow.AddDays(7)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/events/{createdEvent!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify event was actually deleted
        var getResponse = await _client.GetAsync($"/api/events/{createdEvent.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteEvent_WhenEventDoesNotExist_ShouldReturn404NotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/events/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteEvent_WhenDeleted_ShouldNotAppearInGetAll()
    {
        // Arrange - Create two events
        var createRequest1 = new CreateEventRequest
        {
            Title = "Event 1",
            EventDateTime = DateTime.UtcNow.AddDays(7)
        };
        var createRequest2 = new CreateEventRequest
        {
            Title = "Event 2",
            EventDateTime = DateTime.UtcNow.AddDays(8)
        };

        var createResponse1 = await _client.PostAsJsonAsync("/api/events", createRequest1);
        var createResponse2 = await _client.PostAsJsonAsync("/api/events", createRequest2);
        createResponse1.EnsureSuccessStatusCode();
        createResponse2.EnsureSuccessStatusCode();

        var createdEvent1 = await createResponse1.Content.ReadFromJsonAsync<EventResponse>();
        var createdEvent2 = await createResponse2.Content.ReadFromJsonAsync<EventResponse>();

        // Act - Delete one event
        var deleteResponse = await _client.DeleteAsync($"/api/events/{createdEvent1!.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        // Assert - Verify deleted event doesn't appear in the list
        var getAllResponse = await _client.GetAsync("/api/events");
        getAllResponse.EnsureSuccessStatusCode();
        var allEvents = await getAllResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        
        allEvents.Should().NotBeNull();
        allEvents!.Should().NotContain(e => e.Id == createdEvent1.Id);
        allEvents.Should().Contain(e => e.Id == createdEvent2!.Id);
    }
}
