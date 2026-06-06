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
public class PollsControllerTests
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
            Description = "For poll testing",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            Address = "123 Test Street"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    private async Task<PollResponse> CreateTestPollAsync(int eventId, bool allowMultiple = false)
    {
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        var response = await auth.PostAsJsonAsync($"{EventsApiPath}/{eventId}/polls", new CreatePollRequest
        {
            Question = "What time works best?",
            Options = new List<string> { "Morning", "Afternoon", "Evening" },
            AllowMultiple = allowMultiple
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PollResponse>())!;
    }

    private string PollsPath(int eventId) => $"{EventsApiPath}/{eventId}/polls";
    private string PollPath(int eventId, int pollId) => $"{EventsApiPath}/{eventId}/polls/{pollId}";
    private string VotePath(int eventId, int pollId) => $"{EventsApiPath}/{eventId}/polls/{pollId}/votes";
    private string ResultsPath(int eventId, int pollId) => $"{EventsApiPath}/{eventId}/polls/{pollId}/results";

    // ── GET /api/events/{eventId}/polls ────────────────────────────────────────

    [Test]
    public async Task GetPolls_WhenEventExistsAndHasNoPolls_ShouldReturn200WithEmptyList()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var response = await _client.GetAsync(PollsPath(evt.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var polls = await response.Content.ReadFromJsonAsync<List<PollResponse>>();
        polls.Should().BeEmpty();
    }

    [Test]
    public async Task GetPolls_WhenEventHasPolls_ShouldReturn200WithPolls()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        await CreateTestPollAsync(evt.Id);
        await CreateTestPollAsync(evt.Id);

        // Act
        var response = await _client.GetAsync(PollsPath(evt.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var polls = await response.Content.ReadFromJsonAsync<List<PollResponse>>();
        polls.Should().HaveCount(2);
    }

    [Test]
    public async Task GetPolls_WhenEventDoesNotExist_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync(PollsPath(999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/events/{eventId}/polls ───────────────────────────────────────

    [Test]
    public async Task CreatePoll_WhenAuthenticatedAndValid_ShouldReturn201WithPoll()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        var request = new CreatePollRequest
        {
            Question = "Favourite colour?",
            Options = new List<string> { "Red", "Blue", "Green" },
            AllowMultiple = false
        };

        // Act
        var response = await auth.PostAsJsonAsync(PollsPath(evt.Id), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var poll = await response.Content.ReadFromJsonAsync<PollResponse>();
        poll.Should().NotBeNull();
        poll!.Id.Should().BeGreaterThan(0);
        poll.EventId.Should().Be(evt.Id);
        poll.Question.Should().Be("Favourite colour?");
        poll.Options.Should().BeEquivalentTo(new[] { "Red", "Blue", "Green" });
    }

    [Test]
    public async Task CreatePoll_WhenUnauthenticated_ShouldReturn401()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var response = await _client.PostAsJsonAsync(PollsPath(evt.Id), new CreatePollRequest
        {
            Question = "Test?",
            Options = new List<string> { "Yes", "No" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreatePoll_WhenGuestAndGuestPollsEnabled_ShouldReturn201()
    {
        // Arrange — admin creates an event that allows guest polls
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);
        var createEventResponse = await auth.PostAsJsonAsync(EventsApiPath, new CreateEventRequest
        {
            Title = "Open Poll Event",
            AllowGuestPolls = true
        });
        createEventResponse.EnsureSuccessStatusCode();
        var evt = (await createEventResponse.Content.ReadFromJsonAsync<EventResponse>())!;

        // Act — an unauthenticated guest creates a poll
        var response = await _client.PostAsJsonAsync(PollsPath(evt.Id), new CreatePollRequest
        {
            Question = "Pizza or tacos?",
            Options = new List<string> { "Pizza", "Tacos" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var poll = await response.Content.ReadFromJsonAsync<PollResponse>();
        poll!.Question.Should().Be("Pizza or tacos?");
    }

    [Test]
    public async Task CreatePoll_WhenEventDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.PostAsJsonAsync(PollsPath(999), new CreatePollRequest
        {
            Question = "Test?",
            Options = new List<string> { "Yes", "No" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreatePoll_WhenFewerThanTwoOptions_ShouldReturn400()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.PostAsJsonAsync(PollsPath(evt.Id), new CreatePollRequest
        {
            Question = "Test?",
            Options = new List<string> { "Only one" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/events/{eventId}/polls/{pollId} ───────────────────────────────

    [Test]
    public async Task UpdatePoll_WhenAuthenticatedAndValid_ShouldReturn200WithUpdatedPoll()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id);
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.PutAsJsonAsync(PollPath(evt.Id, poll.Id), new UpdatePollRequest
        {
            Question = "Updated question?",
            Options = new List<string> { "New A", "New B" },
            AllowMultiple = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PollResponse>();
        updated!.Question.Should().Be("Updated question?");
        updated.AllowMultiple.Should().BeTrue();
    }

    [Test]
    public async Task UpdatePoll_WhenUnauthenticated_ShouldReturn401()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id);

        // Act
        var response = await _client.PutAsJsonAsync(PollPath(evt.Id, poll.Id), new UpdatePollRequest
        {
            Question = "Updated?",
            Options = new List<string> { "A", "B" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdatePoll_WhenPollDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.PutAsJsonAsync(PollPath(evt.Id, 999), new UpdatePollRequest
        {
            Question = "Updated?",
            Options = new List<string> { "A", "B" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/events/{eventId}/polls/{pollId} ────────────────────────────

    [Test]
    public async Task DeletePoll_WhenAuthenticatedAndExists_ShouldReturn200()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id);
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.DeleteAsync(PollPath(evt.Id, poll.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DeletePoll_WhenDeleted_ShouldNoLongerAppearInGetPolls()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id);
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        await auth.DeleteAsync(PollPath(evt.Id, poll.Id));

        // Assert
        var getResponse = await _client.GetAsync(PollsPath(evt.Id));
        var polls = await getResponse.Content.ReadFromJsonAsync<List<PollResponse>>();
        polls.Should().NotContain(p => p.Id == poll.Id);
    }

    [Test]
    public async Task DeletePoll_WhenUnauthenticated_ShouldReturn401()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id);

        // Act
        var response = await _client.DeleteAsync(PollPath(evt.Id, poll.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeletePoll_WhenPollDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var token = await GetAdminTokenAsync();
        using var auth = CreateAuthenticatedClient(token);

        // Act
        var response = await auth.DeleteAsync(PollPath(evt.Id, 999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/events/{eventId}/polls/{pollId}/votes ────────────────────────

    [Test]
    public async Task SubmitVote_WhenValidSingleChoice_ShouldReturn200()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id, allowMultiple: false);

        // Act
        var response = await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest
        {
            SelectedOptions = new List<int> { 0 }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task SubmitVote_WhenValidMultiChoice_ShouldReturn200()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id, allowMultiple: true);

        // Act
        var response = await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest
        {
            SelectedOptions = new List<int> { 0, 2 }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task SubmitVote_WhenMultipleOptionsOnSingleChoicePoll_ShouldReturn400()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id, allowMultiple: false);

        // Act
        var response = await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest
        {
            SelectedOptions = new List<int> { 0, 1 }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SubmitVote_WhenOptionIndexOutOfBounds_ShouldReturn400()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id); // 3 options: indices 0-2

        // Act
        var response = await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest
        {
            SelectedOptions = new List<int> { 99 }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SubmitVote_WhenPollDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var response = await _client.PostAsJsonAsync(VotePath(evt.Id, 999), new SubmitVoteRequest
        {
            SelectedOptions = new List<int> { 0 }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/events/{eventId}/polls/{pollId}/results ──────────────────────

    [Test]
    public async Task GetPollResults_WhenNoVotes_ShouldReturn200WithZeroTotals()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id);

        // Act
        var response = await _client.GetAsync(ResultsPath(evt.Id, poll.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<PollResultsResponse>();
        results.Should().NotBeNull();
        results!.PollId.Should().Be(poll.Id);
        results.TotalVotes.Should().Be(0);
        results.OptionVotes.Values.Should().AllSatisfy(count => count.Should().Be(0));
    }

    [Test]
    public async Task GetPollResults_AfterVotesSubmitted_ShouldReflectCorrectCounts()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var poll = await CreateTestPollAsync(evt.Id, allowMultiple: false);

        // Submit 2 votes for option 0, 1 vote for option 1
        await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } });
        await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } });
        await _client.PostAsJsonAsync(VotePath(evt.Id, poll.Id), new SubmitVoteRequest { SelectedOptions = new List<int> { 1 } });

        // Act
        var response = await _client.GetAsync(ResultsPath(evt.Id, poll.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<PollResultsResponse>();
        results!.TotalVotes.Should().Be(3);
        results.OptionVotes["0"].Should().Be(2);
        results.OptionVotes["1"].Should().Be(1);
        results.OptionVotes["2"].Should().Be(0);
    }

    [Test]
    public async Task GetPollResults_WhenPollDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var response = await _client.GetAsync(ResultsPath(evt.Id, 999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
