using CompetitionSyncServer.Models;
using CompetitionSyncServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(_ => new HttpClient());
builder.Services.AddSingleton<CompetitionCatalogService>();
builder.Services.AddSingleton<VideoRepository>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapOpenApi();
app.UseCors("AllowAll");

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "CompetitionSyncServer",
    status = "ok",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/api/competitions", async (CompetitionCatalogService catalogService, CancellationToken ct) =>
{
    var competitions = await catalogService.GetCompetitionsAsync(ct);
    return Results.Ok(competitions);
});

app.MapGet("/api/videos", async (VideoRepository repository, CancellationToken ct) =>
{
    var videos = await repository.GetAllAsync(ct);
    return Results.Ok(videos);
});

app.MapPost("/api/videos", async (MatchVideo video, VideoRepository repository, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(video.CompetitionName)
        || string.IsNullOrWhiteSpace(video.MatchName)
        || string.IsNullOrWhiteSpace(video.VideoUrl)
        || !Uri.TryCreate(video.VideoUrl, UriKind.Absolute, out _))
    {
        return Results.BadRequest("Invalid payload.");
    }

    if (video.PublishedAtUtc == default)
    {
        video.PublishedAtUtc = DateTime.UtcNow;
    }

    if (video.Id == Guid.Empty)
    {
        video.Id = Guid.NewGuid();
    }

    await repository.AddOrUpdateAsync(video, ct);
    return Results.Ok(video);
});

app.MapDelete("/api/videos/{id:guid}", async (Guid id, VideoRepository repository, CancellationToken ct) =>
{
    await repository.DeleteAsync(id, ct);
    return Results.NoContent();
});

app.Run();
