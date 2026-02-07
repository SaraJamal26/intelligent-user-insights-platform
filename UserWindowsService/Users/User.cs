namespace UserWindowsService.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // AI fields (filled later)
    public double? SentimentScore { get; set; }
    public List<string>? Tags { get; set; }
    public DateTime? LastAnalyzedAt { get; set; }
    public string? EngagementLevel { get; set; }
}
