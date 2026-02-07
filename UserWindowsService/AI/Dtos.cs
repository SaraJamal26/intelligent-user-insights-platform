namespace UserWindowsService.AI;

public sealed record SentimentRequest(string Text);
public sealed record SentimentResponse(double SentimentScore, string Label, bool? Fallback);

public sealed record TagsRequest(string Text);
public sealed record TagsResponse(List<string> Tags, bool? Fallback);

public sealed record InsightsRequest(string FirstName, string LastName, string Email, string Notes);
public sealed record InsightsResponse(string Summary, string EngagementLevel, List<string> RecommendedActions, bool? Fallback);
