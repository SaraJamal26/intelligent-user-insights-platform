using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using UserWindowsService.AI;

namespace UserService.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _contentRoot;

    public CustomWebApplicationFactory()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "UserServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(_contentRoot);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAiClient>();
            services.AddSingleton<IAiClient, FakeAiClient>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(_contentRoot, true); } catch { }
    }
}

internal sealed class FakeAiClient : IAiClient
{
    public Task<SentimentResponse> GetSentimentAsync(string text, CancellationToken ct)
        => Task.FromResult(new SentimentResponse(0.4, "Neutral", false));

    public Task<TagsResponse> GetTagsAsync(string text, CancellationToken ct)
        => Task.FromResult(new TagsResponse(new List<string> { "support", "stability" }, false));

    public Task<InsightsResponse> GetInsightsAsync(InsightsRequest request, CancellationToken ct)
        => Task.FromResult(new InsightsResponse("Summary", "Medium", new List<string> { "Follow up" }, false));
}
