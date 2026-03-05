using CompetitionSyncServer.Models;
using System.Text.Json;

namespace CompetitionSyncServer.Services;

public sealed class NotificationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _storagePath;

    public NotificationRepository(IConfiguration configuration)
    {
        var fromConfig = configuration["Storage:NotificationsFilePath"];
        _storagePath = string.IsNullOrWhiteSpace(fromConfig)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "notifications.json")
            : fromConfig;
    }

    public async Task<IReadOnlyList<ServerNotification>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var all = await ReadInternalAsync(cancellationToken);
            return all.Where(n => n.CreatedAtUtc > sinceUtc).OrderBy(n => n.CreatedAtUtc).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddAsync(ServerNotification notification, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var list = await ReadInternalAsync(cancellationToken);
            list.Add(notification);
            list = list.OrderByDescending(n => n.CreatedAtUtc).Take(2000).ToList();
            await SaveInternalAsync(list, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<ServerNotification>> ReadInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return new List<ServerNotification>();
        }

        await using var stream = File.OpenRead(_storagePath);
        var notifications = await JsonSerializer.DeserializeAsync<List<ServerNotification>>(stream, JsonOptions, cancellationToken);
        return notifications ?? new List<ServerNotification>();
    }

    private async Task SaveInternalAsync(List<ServerNotification> notifications, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, notifications, JsonOptions, cancellationToken);
    }
}
