namespace UserWindowsService.AI;

public interface IAiClient
{
    Task<SentimentResponse> GetSentimentAsync(string text, CancellationToken ct);
    Task<TagsResponse> GetTagsAsync(string text, CancellationToken ct);
    Task<InsightsResponse> GetInsightsAsync(InsightsRequest request, CancellationToken ct);
}
