using System.Net.Http.Json;

namespace UserWindowsService.AI;

public class AiClient(HttpClient http, IHttpContextAccessor accessor) : IAiClient
{
    private string? CorrelationId =>
        accessor.HttpContext?.Request.Headers["X-Correlation-ID"].ToString();

    public async Task<SentimentResponse> GetSentimentAsync(string text, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/ai/sentiment")
        {
            Content = JsonContent.Create(new SentimentRequest(text))
        };
        if (!string.IsNullOrWhiteSpace(CorrelationId))
            req.Headers.TryAddWithoutValidation("X-Correlation-ID", CorrelationId);

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SentimentResponse>(cancellationToken: ct))!;
    }

    public async Task<TagsResponse> GetTagsAsync(string text, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/ai/tags")
        {
            Content = JsonContent.Create(new TagsRequest(text))
        };
        if (!string.IsNullOrWhiteSpace(CorrelationId))
            req.Headers.TryAddWithoutValidation("X-Correlation-ID", CorrelationId);

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TagsResponse>(cancellationToken: ct))!;
    }

    public async Task<InsightsResponse> GetInsightsAsync(InsightsRequest request, CancellationToken ct)
    {
       using var req = new HttpRequestMessage(HttpMethod.Post, "/api/ai/insights")
        {
            Content = JsonContent.Create(request)
        };
        if (!string.IsNullOrWhiteSpace(CorrelationId))
            req.Headers.TryAddWithoutValidation("X-Correlation-ID", CorrelationId);

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<InsightsResponse>(cancellationToken: ct))!;
    }
}
