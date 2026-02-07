using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using UserWindowsService.Users;

namespace UserService.Tests;

public class UsersApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_users_ShouldCreateUser_AndGetShouldReturnIt()
    {
        var newUser = new User
        {
            FirstName = "Sara",
            LastName = "Jamal",
            Email = "sara.jamal@test.com",
            Notes = "Good support but app crashes sometimes."
        };

        var postResp = await _client.PostAsJsonAsync("/api/users", newUser);
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await postResp.Content.ReadFromJsonAsync<User>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        var getResp = await _client.GetAsync($"/api/users/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Analyze_ShouldEnrichUser()
    {
        var newUser = new User
        {
            FirstName = "Ali",
            LastName = "Khan",
            Email = "ali.khan@test.com",
            Notes = "Service is fast but UI is confusing."
        };

        var postResp = await _client.PostAsJsonAsync("/api/users", newUser);
        var created = await postResp.Content.ReadFromJsonAsync<User>();

        var analyzeResp = await _client.PostAsync($"/api/users/{created!.Id}/analyze", null);
        analyzeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await _client.GetAsync($"/api/users/{created.Id}");
        var fetched = await getResp.Content.ReadFromJsonAsync<User>();

        fetched!.SentimentScore.Should().NotBeNull();
        fetched.Tags.Should().NotBeNull();
        fetched.LastAnalyzedAt.Should().NotBeNull();
        fetched.EngagementLevel.Should().NotBeNull();
    }
}
