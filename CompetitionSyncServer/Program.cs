using CompetitionSyncServer.Models;
using CompetitionSyncServer.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://*:{renderPort}");
}

builder.Services.AddOpenApi();
builder.Services.AddSingleton(_ => new HttpClient());
builder.Services.AddSingleton<CompetitionCatalogService>();
builder.Services.AddSingleton<VideoRepository>();
builder.Services.AddSingleton<NotificationRepository>();
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

app.MapGet("/", (HttpRequest request) =>
{
    request.Query.TryGetValue("status", out var status);
    request.Query.TryGetValue("message", out var message);
    var html = BuildAdminPageHtml(status.ToString(), message.ToString());
    return Results.Content(html, "text/html");
});

app.MapPost("/admin/notifications/send", async (HttpRequest request, NotificationRepository repository, IConfiguration configuration, CancellationToken ct) =>
{
    var form = await request.ReadFormAsync(ct);
    var title = (form["title"].ToString() ?? string.Empty).Trim();
    var message = (form["message"].ToString() ?? string.Empty).Trim();
    var adminCode = (form["adminCode"].ToString() ?? string.Empty).Trim();

    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
    {
        return Results.Redirect("/?status=error&message=Le%20titre%20et%20le%20message%20sont%20obligatoires");
    }

    var expectedAdminKey = configuration["Notifications:AdminKey"];
    if (string.IsNullOrWhiteSpace(expectedAdminKey))
    {
        return Results.Redirect("/?status=error&message=AdminKey%20non%20configure%20sur%20le%20serveur");
    }

    if (!string.Equals(adminCode, expectedAdminKey, StringComparison.Ordinal))
    {
        return Results.Redirect("/?status=error&message=Code%20admin%20invalide");
    }

    var notification = new ServerNotification
    {
        Title = title,
        Message = message,
        CreatedAtUtc = DateTime.UtcNow
    };

    await repository.AddAsync(notification, ct);
    return Results.Redirect("/?status=success&message=Notification%20envoyee");
});

app.MapGet("/health", () => Results.Redirect("/api/health"));

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

app.MapPost("/api/videos/{id:guid}/comments", async (Guid id, VideoComment comment, VideoRepository repository, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(comment.Message))
    {
        return Results.BadRequest("Comment message is required.");
    }

    if (comment.Id == Guid.Empty)
    {
        comment.Id = Guid.NewGuid();
    }

    if (comment.CreatedAtUtc == default)
    {
        comment.CreatedAtUtc = DateTime.UtcNow;
    }

    if (string.IsNullOrWhiteSpace(comment.Author))
    {
        comment.Author = "Utilisateur";
    }

    try
    {
        await repository.AddCommentAsync(id, comment, ct);
        return Results.Ok(comment);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
});

app.MapGet("/api/notifications", async (string? sinceUtc, NotificationRepository repository, CancellationToken ct) =>
{
    var since = DateTime.MinValue;
    if (!string.IsNullOrWhiteSpace(sinceUtc) && DateTime.TryParse(sinceUtc, out var parsed))
    {
        since = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }

    var notifications = await repository.GetSinceAsync(since, ct);
    return Results.Ok(notifications);
});

app.MapPost("/api/notifications/broadcast", async (BroadcastNotificationRequest request, NotificationRepository repository, IConfiguration configuration, HttpRequest httpRequest, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest("Title and message are required.");
    }

    var adminKey = configuration["Notifications:AdminKey"];
    if (!string.IsNullOrWhiteSpace(adminKey))
    {
        if (!httpRequest.Headers.TryGetValue("X-Admin-Key", out var providedKey) || providedKey != adminKey)
        {
            return Results.Unauthorized();
        }
    }

    var notification = new ServerNotification
    {
        Title = request.Title.Trim(),
        Message = request.Message.Trim(),
        CreatedAtUtc = DateTime.UtcNow
    };

    await repository.AddAsync(notification, ct);
    return Results.Ok(notification);
});

app.Run();

static string BuildAdminPageHtml(string? status, string? message)
{
        var alertClass = status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true
                ? "alert success"
                : "alert error";

        var alertHtml = string.IsNullOrWhiteSpace(message)
                ? string.Empty
                : $"<div class='{alertClass}'>{WebUtility.HtmlEncode(message)}</div>";

        return $$"""
<!doctype html>
<html lang="fr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Competition Sync Server</title>
    <style>
        :root {
            --bg: #0c1320;
            --card: #152338;
            --text: #e9f0f7;
            --muted: #9fb2c8;
            --accent: #1fb890;
            --accent-2: #117a67;
            --danger: #b83545;
            --ok: #1b8f56;
            --stroke: #294361;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: "Segoe UI", "Noto Sans", sans-serif;
            background: radial-gradient(circle at top left, #1b3553, var(--bg) 45%);
            color: var(--text);
            min-height: 100vh;
            display: grid;
            place-items: center;
            padding: 20px;
        }
        .panel {
            width: min(760px, 100%);
            background: linear-gradient(180deg, #1a2c45, var(--card));
            border: 1px solid var(--stroke);
            border-radius: 16px;
            padding: 22px;
            box-shadow: 0 12px 30px rgba(0, 0, 0, .35);
        }
        h1 { margin: 0 0 8px; font-size: 30px; }
        p.desc { margin: 0 0 18px; color: var(--muted); }
        .alert {
            border-radius: 10px;
            padding: 10px 12px;
            margin-bottom: 14px;
            border: 1px solid transparent;
            font-size: 14px;
        }
        .alert.success { background: rgba(27,143,86,.18); border-color: var(--ok); }
        .alert.error { background: rgba(184,53,69,.18); border-color: var(--danger); }
        label { display: block; font-size: 14px; margin: 12px 0 6px; color: var(--muted); }
        input, textarea {
            width: 100%;
            padding: 11px 12px;
            border-radius: 10px;
            border: 1px solid var(--stroke);
            background: #0f1c2e;
            color: var(--text);
            font-size: 15px;
        }
        textarea { min-height: 100px; resize: vertical; }
        button {
            margin-top: 14px;
            border: 0;
            background: linear-gradient(135deg, var(--accent), var(--accent-2));
            color: #fff;
            padding: 11px 15px;
            font-size: 15px;
            border-radius: 10px;
            cursor: pointer;
            font-weight: 600;
        }
        .meta { margin-top: 18px; font-size: 13px; color: var(--muted); }
        code { color: #9ae6d8; }
    </style>
</head>
<body>
    <main class="panel">
        <h1>Competition Sync Server</h1>
        <p class="desc">Panneau admin pour diffuser une notification aux utilisateurs de l'app.</p>
        {{alertHtml}}
        <form method="post" action="/admin/notifications/send">
            <label for="title">Titre</label>
            <input id="title" name="title" maxlength="120" required />

            <label for="message">Message</label>
            <textarea id="message" name="message" maxlength="500" required></textarea>

            <label for="adminCode">Code admin</label>
            <input id="adminCode" name="adminCode" type="password" required />

            <button type="submit">Envoyer la notification</button>
        </form>

        <div class="meta">
            API dispo: <code>/api/health</code>, <code>/api/videos</code>, <code>/api/notifications</code>.
        </div>
    </main>
</body>
</html>
""";
}
