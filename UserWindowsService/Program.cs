using Microsoft.Extensions.Hosting.WindowsServices;
using UserWindowsService.Persistence;
using UserWindowsService.Startup;
using UserWindowsService.Users;
using UserWindowsService.AI;


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddSingleton<FileUserRepository>();
builder.Services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<FileUserRepository>());
builder.Services.AddHostedService<RepositoryInitializer>();
builder.Services.AddHttpContextAccessor();


builder.Services.AddHttpClient<IAiClient, AiClient>((sp, http) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["AiService:BaseUrl"] ?? "http://localhost:3001";
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromSeconds(120);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.Use(async (ctx, next) =>
{
    const string header = "X-Correlation-ID";

    var correlationId = ctx.Request.Headers.TryGetValue(header, out var cid) && !string.IsNullOrWhiteSpace(cid)
        ? cid.ToString()
        : Guid.NewGuid().ToString("N");

    ctx.Items[header] = correlationId;
    ctx.Response.Headers[header] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        await next();
    }
});


//app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health", async (IAiClient ai, CancellationToken ct) =>
{
    try
    {
        // cheapest "ping" is to hit the Node health directly with HttpClient,
        // but we only have IAiClient. So add a lightweight tags call with empty safe text:
        var tags = await ai.GetTagsAsync("health-check", ct);
        return Results.Ok(new { status = "ok", ai = "reachable" });
    }
    catch
    {
        // Kubernetes readiness: you can return 503 when dependency down
        return Results.StatusCode(503);
    }
});

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.MapGet("/health/ready", async (IUserRepository repo, IAiClient ai, CancellationToken ct) =>
{
    try
    {
        // repo check (disk load already done at startup; just ensure it can read)
        _ = await repo.GetAllAsync(ct);

        // AI dependency check (cheap call)
        _ = await ai.GetTagsAsync("ready-check", ct);

        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});



// CRUD
app.MapGet("/api/users", async (IUserRepository repo, CancellationToken ct)
    => Results.Ok(await repo.GetAllAsync(ct)));

app.MapGet("/api/users/{id:guid}", async (Guid id, IUserRepository repo, CancellationToken ct) =>
{
    var user = await repo.GetByIdAsync(id, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPost("/api/users", async (User user, IUserRepository repo, CancellationToken ct) =>
{
    await repo.AddAsync(user, ct);
    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapPut("/api/users/{id:guid}", async (Guid id, User input, IUserRepository repo, CancellationToken ct) =>
{
    var existing = await repo.GetByIdAsync(id, ct);
    if (existing is null) return Results.NotFound();

    existing.FirstName = input.FirstName;
    existing.LastName = input.LastName;
    existing.Email = input.Email;
    existing.Notes = input.Notes;

    await repo.UpdateAsync(existing, ct);
    return Results.Ok(existing);
});

app.MapDelete("/api/users/{id:guid}", async (Guid id, IUserRepository repo, CancellationToken ct) =>
{
    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
});

// AI

app.MapPost("/api/users/{id:guid}/analyze", async (
    Guid id,
    IUserRepository repo,
    IAiClient ai,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var log = loggerFactory.CreateLogger("AnalyzeUser");

    var user = await repo.GetByIdAsync(id, ct);
    if (user is null) return Results.NotFound();

    var notes = user.Notes ?? "";

    try
    {
        var sentiment = await ai.GetSentimentAsync(notes, ct);
        var tags = await ai.GetTagsAsync(notes, ct);
        var insights = await ai.GetInsightsAsync(
            new InsightsRequest(user.FirstName, user.LastName, user.Email, notes),
            ct);

        user.SentimentScore = sentiment.SentimentScore;
        user.Tags = tags.Tags;
        user.EngagementLevel = insights.EngagementLevel;
        user.LastAnalyzedAt = DateTime.UtcNow;

        await repo.UpdateAsync(user, ct);

        return Results.Ok(new
        {
            user.Id,
            user.SentimentScore,
            sentiment.Label,
            user.Tags,
            user.EngagementLevel,
            user.LastAnalyzedAt,
            insights.Summary,
            insights.RecommendedActions,
            sentiment.Fallback,
            //tags.Fallback,
            //insights.Fallback
        });
    }
    catch (Exception ex)
    {
        // “Proper error handling and fallback strategies” per PDF :contentReference[oaicite:1]{index=1}
        log.LogError(ex, "AI analysis failed for user {UserId}", id);
        return Results.Problem("AI analysis failed. Please try again later.");
    }
});

app.MapGet("/api/users/insights", async (
    IUserRepository repo,
    CancellationToken ct) =>
{
    var users = await repo.GetAllAsync(ct);

    // Example: basic insight without calling AI for every user (fast + safe)
    var analyzed = users.Count(u => u.LastAnalyzedAt != null);
    var avgSentiment = users.Where(u => u.SentimentScore != null).Select(u => u.SentimentScore!.Value).DefaultIfEmpty(0).Average();

    var topTags = users
        .Where(u => u.Tags != null)
        .SelectMany(u => u.Tags!)
        .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Count())
        .Take(10)
        .Select(g => new { tag = g.Key, count = g.Count() })
        .ToList();

    return Results.Ok(new
    {
        totalUsers = users.Count,
        analyzedUsers = analyzed,
        averageSentiment = avgSentiment,
        topTags
    });
});



app.Run();
