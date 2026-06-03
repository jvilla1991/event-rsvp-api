using System.Net;
using System.Net.Http.Json;
using EventRsvp.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using EventRsvp.Infrastructure.Data;

namespace EventRsvp.Api.Tests.Controllers;

[TestFixture]
public class InvitesControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

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
                    var dbContextOptionsDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<EventRsvpDbContext>));
                    if (dbContextOptionsDescriptor != null)
                        services.Remove(dbContextOptionsDescriptor);

                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(EventRsvpDbContext));
                    if (dbContextDescriptor != null)
                        services.Remove(dbContextDescriptor);

                    services.AddDbContext<EventRsvpDbContext>(options =>
                        options.UseInMemoryDatabase(testDbName));
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

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.Token;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<EventResponse> CreateTestEventAsync()
    {
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        var response = await auth.PostAsJsonAsync(EventsApiPath, new CreateEventRequest
        {
            Title = "Test Event",
            Description = "For invite testing",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            Address = "123 Test Street"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    private async Task<InviteResponse> CreateTestInviteAsync(int eventId, string? name = "Bob")
    {
        // CreateInvite is a public endpoint — no auth needed
        var response = await _client.PostAsJsonAsync($"{EventsApiPath}/{eventId}/invites", new CreateInviteRequest
        {
            Name = name
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InviteResponse>())!;
    }

    private string InvitesPath(int eventId) => $"{EventsApiPath}/{eventId}/invites";
    private string InvitePath(int eventId, int inviteId) => $"{EventsApiPath}/{eventId}/invites/{inviteId}";
    private string ViewPath(string token) => $"/api/invites/{token}";

    // ── GET /api/events/{eventId}/invites ──────────────────────────────────────

    [Test]
    public async Task GetInvites_WhenAuthenticatedAndEventHasNoInvites_ShouldReturn200WithEmptyList()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.GetAsync(InvitesPath(evt.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invites = await response.Content.ReadFromJsonAsync<List<InviteResponse>>();
        invites.Should().BeEmpty();
    }

    [Test]
    public async Task GetInvites_WhenUnauthenticated_ShouldReturn401()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var response = await _client.GetAsync(InvitesPath(evt.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetInvites_WhenEventHasInvites_ShouldReturn200WithInvites()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        await CreateTestInviteAsync(evt.Id, "Alice");
        await CreateTestInviteAsync(evt.Id, "Bob");

        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.GetAsync(InvitesPath(evt.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invites = await response.Content.ReadFromJsonAsync<List<InviteResponse>>();
        invites.Should().HaveCount(2);
        invites!.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Alice", "Bob" });
    }

    [Test]
    public async Task GetInvites_WhenEventDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.GetAsync(InvitesPath(999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/events/{eventId}/invites ────────────────────────────────────

    [Test]
    public async Task CreateInvite_WhenAuthenticatedAndValid_ShouldReturn201WithInvite()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.PostAsJsonAsync(InvitesPath(evt.Id), new CreateInviteRequest
        {
            Name = "Charlie"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await response.Content.ReadFromJsonAsync<InviteResponse>();
        invite.Should().NotBeNull();
        invite!.Id.Should().BeGreaterThan(0);
        invite.EventId.Should().Be(evt.Id);
        invite.Name.Should().Be("Charlie");
        invite.Token.Should().NotBeNullOrWhiteSpace();
        invite.Status.Should().Be("NotOpened");
        invite.ViewedAt.Should().BeNull();
    }

    [Test]
    public async Task CreateInvite_IsPublic_ShouldReturn201WithoutAuthentication()
    {
        // Arrange — anyone on the event page can generate a shareable link
        var evt = await CreateTestEventAsync();

        // Act — unauthenticated client
        var response = await _client.PostAsJsonAsync(InvitesPath(evt.Id), new CreateInviteRequest
        {
            Name = "Dave"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateInvite_WhenNameIsOmitted_ShouldReturn201WithEmptyName()
    {
        // Arrange — Name is optional; an anonymous invite is valid
        var evt = await CreateTestEventAsync();

        // Act
        var response = await _client.PostAsJsonAsync(InvitesPath(evt.Id), new CreateInviteRequest
        {
            Name = null
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await response.Content.ReadFromJsonAsync<InviteResponse>();
        invite!.Name.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task CreateInvite_WhenEventDoesNotExist_ShouldReturn404()
    {
        // Act — no auth required, but event must exist
        var response = await _client.PostAsJsonAsync(InvitesPath(999), new CreateInviteRequest
        {
            Name = "Eve"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateInvite_EachInviteShouldHaveUniqueToken()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var invite1 = await CreateTestInviteAsync(evt.Id, "Frank");
        var invite2 = await CreateTestInviteAsync(evt.Id, "Grace");

        // Assert
        invite1.Token.Should().NotBe(invite2.Token);
    }

    // ── DELETE /api/events/{eventId}/invites/{inviteId} ───────────────────────

    [Test]
    public async Task DeleteInvite_WhenAuthenticatedAndExists_ShouldReturn200()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id);
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.DeleteAsync(InvitePath(evt.Id, invite.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DeleteInvite_WhenDeleted_ShouldNoLongerAppearInGetInvites()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id, "Heidi");
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        await auth.DeleteAsync(InvitePath(evt.Id, invite.Id));

        // Assert
        var getResponse = await auth.GetAsync(InvitesPath(evt.Id));
        var invites = await getResponse.Content.ReadFromJsonAsync<List<InviteResponse>>();
        invites.Should().NotContain(i => i.Id == invite.Id);
    }

    [Test]
    public async Task DeleteInvite_WhenUnauthenticated_ShouldReturn401()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id);

        // Act
        var response = await _client.DeleteAsync(InvitePath(evt.Id, invite.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteInvite_WhenInviteDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.DeleteAsync(InvitePath(evt.Id, 999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/invites/{token} (public view endpoint) ───────────────────────

    [Test]
    public async Task ViewInvite_WhenValidToken_ShouldReturn200WithInviteDetails()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id, "Ivan");

        // Act
        var response = await _client.GetAsync(ViewPath(invite.Token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InviteResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Ivan");
        result.EventId.Should().Be(evt.Id);
        result.Token.Should().Be(invite.Token);
    }

    [Test]
    public async Task ViewInvite_WhenFirstView_ShouldMarkAsViewed()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id, "Judy");

        // Act
        var response = await _client.GetAsync(ViewPath(invite.Token));

        // Assert
        var result = await response.Content.ReadFromJsonAsync<InviteResponse>();
        result!.Status.Should().Be("Opened");
        result.ViewedAt.Should().NotBeNull();
    }

    [Test]
    public async Task ViewInvite_WhenViewedTwice_ShouldPreserveOriginalViewedAt()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id, "Karl");

        // First view
        var firstResponse = await _client.GetAsync(ViewPath(invite.Token));
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // Second view
        var secondResponse = await _client.GetAsync(ViewPath(invite.Token));
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // Assert — ViewedAt should be the same both times
        secondResult!.ViewedAt.Should().Be(firstResult!.ViewedAt);
    }

    [Test]
    public async Task ViewInvite_WhenInviteWasNotViewed_ShouldReflectViewedStatusInGetInvites()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id, "Laura");
        var adminToken = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(adminToken);

        // Check it is unviewed before the recipient opens it
        var beforeList = await (await auth.GetAsync(InvitesPath(evt.Id))).Content.ReadFromJsonAsync<List<InviteResponse>>();
        beforeList!.Single(i => i.Id == invite.Id).Status.Should().Be("NotOpened");

        // Recipient opens the link
        await _client.GetAsync(ViewPath(invite.Token));

        // Admin re-checks the list
        var afterList = await (await auth.GetAsync(InvitesPath(evt.Id))).Content.ReadFromJsonAsync<List<InviteResponse>>();
        afterList!.Single(i => i.Id == invite.Id).Status.Should().Be("Opened");
    }

    [Test]
    public async Task ViewInvite_WhenTokenDoesNotExist_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync(ViewPath("nonexistenttoken12345"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ViewInvite_IsPublic_ShouldNotRequireAuthentication()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var invite = await CreateTestInviteAsync(evt.Id, "Mike");

        // Act — unauthenticated client
        var response = await _client.GetAsync(ViewPath(invite.Token));

        // Assert — public endpoint, no 401
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
