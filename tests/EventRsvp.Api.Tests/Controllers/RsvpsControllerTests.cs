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
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "Pasta Salad" },
            WhiteElephant = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/rsvps", request);

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
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = string.Empty,
            BringingDish = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/rsvps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetRsvps_WhenRsvpsExist_ShouldReturn200Ok()
    {
        // Arrange - Create a test RSVP first
        var createRequest = new CreateRsvpRequest
        {
            Name = "John Doe",
            BringingDish = false
        };
        var createResponse = await _client.PostAsJsonAsync("/api/rsvps", createRequest);
        createResponse.EnsureSuccessStatusCode(); // Ensure the create was successful

        // Act
        var response = await _client.GetAsync("/api/rsvps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<RsvpResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task GetRsvps_WhenNoRsvps_ShouldReturnEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/rsvps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<RsvpResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }
}

