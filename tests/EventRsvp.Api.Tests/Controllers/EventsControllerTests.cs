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
    
    private const int NonExistentEventId = 999;
    private const int InvalidEventId = 0;
    private const int NegativeEventId = -1;
    private const string EventsApiPath = "/api/events";

    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        
        var testDbName = $"TestDb_{Guid.NewGuid()}";
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
        _client?.Dispose();
        _factory?.Dispose();
    }

    private async Task<EventResponse> CreateTestEventAsync(string title = "Test Event", DateTime? eventDateTime = null)
    {
        var token = await GetAdminTokenAsync();
        using var authenticatedClient = CreateAuthenticatedClient(token);
        var request = new CreateEventRequest
        {
            Title = title,
            EventDateTime = eventDateTime ?? DateTime.UtcNow.AddDays(7),
            Description = "Test Description",
            Address = "123 Test Street"
        };

        var response = await authenticatedClient.PostAsJsonAsync(EventsApiPath, request);
        response.EnsureSuccessStatusCode();
        
        var createdEvent = await response.Content.ReadFromJsonAsync<EventResponse>();
        return createdEvent!;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Test]
    public async Task CreateEvent_WhenValidRequestAndAuthenticated_ShouldReturn201Created()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            Address = "123 Test Street"
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync(EventsApiPath, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<EventResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Event");
        result.Id.Should().BeGreaterThan(0);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task CreateEvent_WhenUnauthenticated_ShouldReturn401Unauthorized()
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
        var response = await _client.PostAsJsonAsync(EventsApiPath, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetEvent_WhenEventExists_ShouldReturn200Ok()
    {
        // Arrange
        var createdEvent = await CreateTestEventAsync();

        // Act
        var response = await _client.GetAsync($"{EventsApiPath}/{createdEvent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EventResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdEvent.Id);
        result.Title.Should().Be(createdEvent.Title);
    }

    [Test]
    public async Task GetEvent_WhenEventDoesNotExist_ShouldReturn404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"{EventsApiPath}/{NonExistentEventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteEvent_WhenEventExistsAndAuthenticated_ShouldReturn204NoContent()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        var createdEvent = await CreateTestEventAsync("Event to Delete");

        // Act
        var deleteResponse = await authenticatedClient.DeleteAsync($"{EventsApiPath}/{createdEvent.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify event was deleted
        var getResponse = await _client.GetAsync($"{EventsApiPath}/{createdEvent.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task DeleteEvent_WhenEventDoesNotExistAndAuthenticated_ShouldReturn404NotFound()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);

        // Act
        var response = await authenticatedClient.DeleteAsync($"{EventsApiPath}/{NonExistentEventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task DeleteEvent_WhenIdIsZeroAndAuthenticated_ShouldReturn400BadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);

        // Act
        var response = await authenticatedClient.DeleteAsync($"{EventsApiPath}/{InvalidEventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task DeleteEvent_WhenIdIsNegativeAndAuthenticated_ShouldReturn400BadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);

        // Act
        var response = await authenticatedClient.DeleteAsync($"{EventsApiPath}/{NegativeEventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task DeleteEvent_WhenDeleted_ShouldNotAppearInGetAll()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        
        var createdEvent1 = await CreateTestEventAsync("Event 1");
        var createdEvent2 = await CreateTestEventAsync("Event 2");

        // Act
        var deleteResponse = await authenticatedClient.DeleteAsync($"{EventsApiPath}/{createdEvent1.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        // Assert - Verifying deleted event doesn't appear in the list
        var getAllResponse = await _client.GetAsync(EventsApiPath);
        getAllResponse.EnsureSuccessStatusCode();
        var allEvents = await getAllResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        
        allEvents.Should().NotBeNull();
        allEvents!.Should().NotContain(e => e.Id == createdEvent1.Id);
        allEvents.Should().Contain(e => e.Id == createdEvent2.Id);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task UpdateEvent_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        var createdEvent = await CreateTestEventAsync("Original Title");
        
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        // Act - Try to update without authentication
        var response = await _client.PutAsJsonAsync($"{EventsApiPath}/{createdEvent.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task UpdateEvent_WhenAuthenticated_ShouldReturn200Ok()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        var createdEvent = await CreateTestEventAsync("Original Title");
        
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            EventDateTime = DateTime.UtcNow.AddDays(14),
            Address = "456 Updated Street"
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"{EventsApiPath}/{createdEvent.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EventResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.Address.Should().Be("456 Updated Street");
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task DeleteEvent_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        var createdEvent = await CreateTestEventAsync("Event to Delete");

        // Act - Try to delete without authentication
        var response = await _client.DeleteAsync($"{EventsApiPath}/{createdEvent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task DeleteEvent_WhenAuthenticated_ShouldReturn204NoContent()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authenticatedClient = CreateAuthenticatedClient(token);
        var createdEvent = await CreateTestEventAsync("Event to Delete");

        // Act
        var deleteResponse = await authenticatedClient.DeleteAsync($"{EventsApiPath}/{createdEvent.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        authenticatedClient.Dispose();
    }

    [Test]
    public async Task CreateEvent_WhenInvalidToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        var authenticatedClient = CreateAuthenticatedClient(invalidToken);
        
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            Address = "123 Test Street"
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync(EventsApiPath, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        authenticatedClient.Dispose();
    }
}
